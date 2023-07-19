using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CaseExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nexus.Config;
using Nexus.Extensions;
using Nexus.Models;
using Nexus.Runners;
using YamlDotNet.Serialization;
using static Nexus.Extensions.ConsoleUtilities;
using static Nexus.Extensions.DirectoryExtensions;

namespace Nexus.Services;

public class SolutionGenerator
{
    private readonly ConfigurationService _configurationService;
    private readonly GitHubService _gitHubService;

    public SolutionGenerator()
    {
        _configurationService = new ConfigurationService();
        _gitHubService = new GitHubService();
    }

    public async Task<bool> InitializeSolution(string rawName, CancellationToken cancellationToken = default)
    {
        string solutionName = NameExtensions.GetKebabCasedNameWithoutApi(rawName);
        
        // Download solution
        string solutionDirectory = _configurationService.GetBasePath();
        await _gitHubService.DownloadSolutionTemplate(solutionName, solutionDirectory, cancellationToken);
        
        // Replace ProjectName in nexus config
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();
        if (config == null)
        {
            return false;
        }

        config.SolutionName = solutionName;
        _configurationService.WriteConfiguration(config);
        
        // Replace names in docker-compose
        string dockerComposePath = _configurationService.GetDockerComposePath(RunType.Docker);

        if (File.Exists(dockerComposePath))
        {
            string[] lines = await File.ReadAllLinesAsync(dockerComposePath, cancellationToken);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"image:\s+nexus.*:latest$"))
                {
                    lines[i] = lines[i].Replace("nexus", solutionName);
                }
            }
            
            await File.WriteAllLinesAsync(dockerComposePath, lines, cancellationToken);
        }
        
        return true;
    }
    
    public async Task<bool> AddService(string rawName)
    {
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            return false;
        }

        int httpsPort = _configurationService.GetNewServicePort();
        int dbPort = _configurationService.GetNewDbPort();
        
        ServiceInitializationInfo info = new (
            solutionName: config.SolutionName,
            serviceNameRaw: rawName,
            basePath: _configurationService.GetBasePath(),
            httpsPort: httpsPort,
            httpPort: httpsPort + 1,
            dbPort);

        if (_configurationService.ServiceExists(info.ServiceNameKebabCaseAndApi))
        { 
            return false;
        }
        
        // Create service folders
        EnsureDirectories(new[] { info.ServiceRootFolder, info.ServiceCsProjectFolder });

        // Download project template
        await _gitHubService.DownloadServiceTemplate(info.ServiceCsProjectFolder);
        
        // Replace variables
        Console.WriteLine("Updating service values");
        ReplaceTemplateVariables(info);
        
        // Update docker compose yml
        // Add DB
        // Add networks
        Console.WriteLine("Updating docker-compose");
        UpdateDockerComposeYaml(info, RunType.Local);
        UpdateDockerComposeYaml(info, RunType.Docker);

        // Update prometheus yml
        Console.WriteLine("Updating prometheus config");
        UpdatePrometheusYaml(info, RunType.Local);
        UpdatePrometheusYaml(info, RunType.Docker);

        Console.WriteLine("Updating env file");
        UpdateEnvironmentFile(info);
        
        // Add service to hc config
        UpdateHcConfig(info);
        
        // Add service to solution
        AddCsProjectFileToSolution(info.SolutionPath, info.ServiceCsProjectFile);
        
        Console.WriteLine("Done");
        return _configurationService.AddService(info);
    }

    private void UpdateHcConfig(ServiceInitializationInfo info)
    {
        Console.WriteLine("Adding service to Health Checks");
        string appConfigPath = Path.Combine(_configurationService.HealthChecksDashboardConsulDirectory, "app-config.json");

        if (!File.Exists(appConfigPath))
        {
            return;
        }

        string appConfigJson = File.ReadAllText(appConfigPath);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            return;
        }
        
        JArray clients = appConfig.HealthCheck.Clients as JArray ?? new JArray();
        JObject newClient = new()
        {
            ["Name"] = info.ServiceNamePascalCasedAndDotApi,
            ["ServiceName"] = info.ServiceNameKebabCaseAndApi,
        };
        clients.Add(newClient);

        appConfig.HealthCheck.Clients = clients;
        string updatedJson = JsonConvert.SerializeObject(appConfig);
        
        File.WriteAllText(appConfigPath, updatedJson);
    }

    private void UpdateEnvironmentFile(ServiceInitializationInfo info)
    {
        string envFilePath = _configurationService.EnvironmentFile;

        if (!File.Exists(envFilePath))
        {
            return;
        }

        StringBuilder sb = new ();
        string connectionString = $"User ID=developer;Password=dev123;Host={info.DbHost};Port={info.DbPort};Database={info.DbName}";
        sb.AppendLine();
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_TOKEN={info.ServiceToken}");
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_CERT_PASSWORD={info.CertificatePassword}");
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_PORT_INTERNAL={info.HttpPort}");
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_PORT_EXTERNAL={info.HttpsPort}");
        sb.AppendLine($"{info.ServiceNameSnakeCase.ToUpperInvariant()}_DB_CONNECTION_STRING=\"{connectionString}\"");
        sb.AppendLine($"{info.ServiceNameSnakeCase.ToUpperInvariant()}_DB_PORT={info.DbPort}");

        string newVars = sb.ToString();
        File.AppendAllText(envFilePath, newVars);
    }

    private void UpdatePrometheusYaml(ServiceInitializationInfo info, RunType runType)
    {
        string ymlFilePath = _configurationService.GetPrometheusFile(runType);

        if (!File.Exists(ymlFilePath))
        {
            return;
        }

        string text = File.ReadAllText(ymlFilePath);
        
        IDeserializer deserializer = new DeserializerBuilder().Build();

        dynamic yamlObject = deserializer.Deserialize<dynamic>(new StringReader(text));
        List<dynamic>? scrapeConfigs = (List<dynamic>)yamlObject["scrape_configs"];
        string target = runType switch
        {
            RunType.Local => $"host.docker.internal:{info.HttpsPort}",
            RunType.Docker => info.ServiceNameKebabCaseAndApi,
            _ => "",
        };

        Dictionary<string, object> newScrapeConfig = new()
        {
            { "job_name", info.ServiceNameKebabCaseAndApi },
            {
                "static_configs", new List<Dictionary<string, object>>
                {
                    new()
                    {
                        { "targets", new List<string> { target } },
                    },
                }
            },
            { "scheme", "https" },
            {
                "tls_config", new Dictionary<string, object>
                {
                    { "insecure_skip_verify", true },
                }
            },
        };
        
        scrapeConfigs.Add(newScrapeConfig);
        ISerializer serializer = new SerializerBuilder().Build();
        string updatedYaml = serializer.Serialize(yamlObject);
        
        File.WriteAllText(ymlFilePath, updatedYaml);
    }

    private void UpdateDockerComposeYaml(ServiceInitializationInfo info, RunType runType)
    {
        string ymlFilePath = _configurationService.GetDockerComposePath(runType);

        if (!File.Exists(ymlFilePath))
        {
            return;
        }

        string dockerComposeText = File.ReadAllText(ymlFilePath);
        string dbHostPortKey = $"{info.DbHost.ToSnakeCase().ToUpper()}_PORT";
        string serviceToAdd = @$"##SERVICES_START##
  {info.DbHost}:
    container_name: {info.DbHost}
    image: postgres:latest
    restart: always
    environment:
      - POSTGRES_USER=developer
      - POSTGRES_PASSWORD=dev123
      - POSTGRES_DB={info.DbName}
      - PGPORT=${{{dbHostPortKey}}}
    ports:
      - ${{{dbHostPortKey}}}:${{{dbHostPortKey}}}
    volumes:
      - {info.DbHost}:/var/lib/postgresql/data
    networks:
      - {info.DbHost}";

        if (runType == RunType.Docker)
        {
            string apiService = @$"
  {info.ServiceNameKebabCaseAndApi}:
    image: {info.SolutionNameSnakeCase}-{info.ServiceNameKebabCaseAndApi}:latest
    depends_on:
      - {info.DbHost}
    restart: always
    deploy:
      replicas: 2
    expose:
      - ${{{info.ServiceNameSnakeCaseAndApi.ToUpper()}_PORT_EXTERNAL}}
    environment:
      - ConsulKV:Url=http://consul-server1:8500
      - ConsulKV:Token=${{{info.ServiceNameSnakeCaseAndApi.ToUpper()}_TOKEN}}
      - Consul:Host=consul-server1
      - GlobalConfiguration:BaseUrl=http://localhost:${{{info.ServiceNameSnakeCaseAndApi.ToUpper()}_PORT_INTERNAL}}
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_HTTPS_PORT=443
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${{{info.ServiceNameSnakeCaseAndApi.ToUpper()}_CERT_PASSWORD}}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
      - ConnectionStrings:Default=${{{info.DbHost.ToSnakeCase().ToUpper()}_CONNECTION_STRING}}
    volumes:
      - .\devcerts:/https/
    networks:
      - econsul
      - {info.ServiceNameKebabCaseAndApi}
      - {info.DbHost}
      - logs
      - tracing
      - api-gateway
";

            serviceToAdd = @$"{serviceToAdd}
{apiService}
";
        }

        string volumeToAdd = $@"##VOLUMES_START##
  {info.DbHost}:
    driver: local";

        string networkToAdd = $@"##NETWORKS_START##
  {info.ServiceNameKebabCaseAndApi}:
    driver: bridge
  {info.DbHost}:
    driver: bridge";

        string outputYaml = dockerComposeText
            .Replace("##SERVICES_START##", serviceToAdd)
            .Replace("##VOLUMES_START##", volumeToAdd)
            .Replace("##NETWORKS_START##", networkToAdd);
        
        File.WriteAllText(ymlFilePath, outputYaml);
    }

    private void ReplaceTemplateVariables(ServiceInitializationInfo info)
    {
        // List files
        string[] files = Directory.GetFiles(info.ServiceCsProjectFolder, "*.*", SearchOption.AllDirectories);

        // Replace variables
        foreach (string file in files)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Unable to find file {file}");
                continue;
            }
            
            string fileText = File.ReadAllText(file);
            string updatedText = fileText
                .Replace("{{RootNamespace}}", info.RootNamespace)
                .Replace("{{ServiceName}}", info.ServiceNameKebabCase) // FIXXXXX
                .Replace("{{ServiceNameKebabCaseAndApi}}", info.ServiceNameKebabCaseAndApi)
                .Replace("{{ProjectName}}", info.ServiceNamePascalCasedAndDotApi)
                .Replace("{{ApplicationPort_Https}}", info.HttpsPort.ToString())
                .Replace("{{ApplicationPort_Http}}", info.HttpPort.ToString())
                .Replace("{{SolutionNameSnakeCase}}", info.SolutionNameSnakeCase)
                .Replace("{{ServiceNameSnakeCase}}", info.ServiceNameSnakeCase);
            
            File.WriteAllText(file, updatedText);
        }
        
        // Rename csproj
        string? csProjFile = files.FirstOrDefault(x => x.EndsWith("ServiceTemplate.csproj"));
        if (csProjFile != null)
        {
            string updatedName = csProjFile.Replace("ServiceTemplate.csproj", $"{info.ServiceNamePascalCasedAndDotApi}.csproj");
            File.Move(csProjFile, updatedName);
        }
    }

    private static bool AddCsProjectFileToSolution(string solutionPath, string csProjectFilePath)
    {
        if (!File.Exists(csProjectFilePath))
        {
            // TODO: Write message
            return false;
        }

        Console.WriteLine($"Adding \"{csProjectFilePath}\" to the solution");
        RunPowershellCommand($"dotnet sln \"{solutionPath}\" add \"{csProjectFilePath}\"");
        return true;
    }

    public async Task<bool> Eject()
    {
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            return false;
        }
        
        string librariesFolder = Path.Combine(_configurationService.GetBasePath(), "libraries");
        if (Directory.Exists(librariesFolder))
        {
            Console.WriteLine("Libraries already exist");
            return false;
        }

        string solutionFile = Path.Combine(_configurationService.GetBasePath(), $"{config.SolutionName}.sln");
        
        // Download libraries
        await _gitHubService.DownloadLibraries(librariesFolder);
        
        // Clean up libraries folder
        CleanupLibrariesFolder(librariesFolder);

        // Add libraries to sln
        AddLibrariesToSolutionFile(librariesFolder, solutionFile);
        
        // Update csproj files
        ReplaceLibrariesInCsProjFiles(librariesFolder, config);
        
        return true;
    }

    private void ReplaceLibrariesInCsProjFiles(string librariesFolder, NexusSolutionConfiguration config)
    {
        string librariesSrcPath = Path.Combine(librariesFolder, "src");
        string[] libraryProjectFiles = Directory.GetFiles(librariesSrcPath, "*.csproj", SearchOption.AllDirectories);
        Dictionary<string, string> packages = libraryProjectFiles
            .Select(x => new KeyValuePair<string, string>(x, Path.GetFileName(x).Replace(".csproj", "")))
            .ToDictionary(x => x.Key, x => x.Value);

        List<string> csProjFilesToUpdate = new ()
        {
            _configurationService.ApiGatewayCsProjFile,
            _configurationService.HealthChecksDashboardCsProjFile,
        };
        
        foreach (NexusServiceConfiguration serviceConfig in config.Services)
        {
            csProjFilesToUpdate.Add(_configurationService.GetServiceCsProjFile(serviceConfig.ServiceName, serviceConfig.ProjectName));
        }

        foreach (string csProj in csProjFilesToUpdate)
        {
            ReplaceLibrariesInCsProjFile(csProj, packages);
        }
    }

    private void ReplaceLibrariesInCsProjFile(string csProjFilePath, Dictionary<string, string> packages)
    {
        if (!File.Exists(csProjFilePath))
        {
            return;
        }

        string? csProjFolderPath = Path.GetDirectoryName(csProjFilePath);

        if (csProjFolderPath == null)
        {
            return;
        }

        XDocument csProjDoc = XDocument.Load(csProjFilePath);
        Dictionary<string, string> packagesToAdd = new ();
        
        foreach (KeyValuePair<string, string> package in packages)
        {
            XElement? packageRefElement = csProjDoc.Root?.Descendants("PackageReference")
                .FirstOrDefault(e => e.Attribute("Include")?.Value == package.Value);
            
            if (packageRefElement != null)
            {
                packageRefElement.Remove();
                packagesToAdd.Add(package.Key, package.Value);
                Console.WriteLine($"Remove {package.Value} from {csProjFilePath}");
            }
        }

        if (packagesToAdd.Count > 0)
        {
            XElement itemGroup = new ("ItemGroup");

            foreach (KeyValuePair<string, string> package in packagesToAdd)
            {
                string path = Path.GetRelativePath(csProjFolderPath, package.Key);
                XElement projectRef = new ("ProjectReference");
                projectRef.SetAttributeValue("Include", path);
                itemGroup.Add(projectRef);
                
                Console.WriteLine($"Added {path} to {csProjFilePath}");
            }

            csProjDoc.Root?.Add(itemGroup);
        }
        
        csProjDoc.Save(csProjFilePath);
    }

    private void AddLibrariesToSolutionFile(string librariesFolder, string solutionPath)
    {
        Console.WriteLine(librariesFolder);
        Console.WriteLine(solutionPath);
        string srcPath = Path.Combine(librariesFolder, "src");
        string testsPath = Path.Combine(librariesFolder, "tests");

        string[] srcProjects = Directory.GetFiles(srcPath, "*.csproj", SearchOption.AllDirectories);
        string[] testProjects = Directory.GetFiles(testsPath, "*.csproj", SearchOption.AllDirectories);
        IEnumerable<string> allProjects = srcProjects.Concat(testProjects);

        foreach (string project in allProjects)
        {
            AddCsProjectFileToSolution(solutionPath, project);
        }
    }

    private void CleanupLibrariesFolder(string librariesFolder)
    {
        // Remove sln file
        string slnFile = Path.Combine(librariesFolder, "nexus-libraries.sln");
        if (File.Exists(slnFile))
        {
            File.Delete(slnFile);
        }

        // Remove global json
        string globalJsonFile = Path.Combine(librariesFolder, "global.json");
        if (File.Exists(globalJsonFile))
        {
            File.Delete(globalJsonFile);
        }

        // Remove .github folder
        string ghFolder = Path.Combine(librariesFolder, ".github");
        if (Directory.Exists(ghFolder))
        {
            Directory.Delete(ghFolder, true);
        }
    }
}
