using System.Text;
using System.Xml.Linq;
using CaseExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nexus.Config;
using Nexus.Extensions;
using Nexus.Models;
using Nexus.Services;
using YamlDotNet.Serialization;
using static Nexus.Extensions.ConsoleUtilities;
using static Nexus.Extensions.DirectoryExtensions;

namespace Nexus.Generators;

public class SolutionGenerator
{
    private readonly ConfigurationService _configurationService;
    private readonly GitHubService _gitHubService;
    private readonly Random _random;

    public SolutionGenerator()
    {
        _configurationService = new ConfigurationService();
        _gitHubService = new GitHubService();
        _random = new Random();
    }

    public async Task<bool> InitializeSolution(string rawName)
    {
        string solutionName = NameExtensions.GetKebabCasedNameWithoutApi(rawName);
        
        // Download solution
        string solutionDirectory = _configurationService.GetBasePath();
        await _gitHubService.DownloadSolutionTemplate(solutionName, solutionDirectory);
        
        // Replace ProjectName in nexus config
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();
        if (config == null)
        {
            return false;
        }

        config.ProjectName = solutionName;
        _configurationService.WriteConfiguration(config);
        
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
            solutionName: config.ProjectName,
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
        UpdateDockerComposeLocalYaml(info);

        // Update prometheus yml
        Console.WriteLine("Updating prometheus config");
        UpdatePrometheusLocalYaml(info);

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
            ["Url"] = $"https://localhost:{info.HttpsPort}/actuator/health",
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
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_TOKEN={info.ServiceToken}");
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_CERT_PASSWORD={info.CertificatePassword}");
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_PORT_INTERNAL={info.HttpPort}");
        sb.AppendLine($"{info.ServiceNameSnakeCaseAndApi.ToUpperInvariant()}_PORT_EXTERNAL={info.HttpsPort}");
        sb.AppendLine($"{info.ServiceNameSnakeCase.ToUpperInvariant()}_DB_CONNECTION_STRING={connectionString}");
        sb.AppendLine($"{info.ServiceNameSnakeCase.ToUpperInvariant()}_DB_PORT={info.DbPort}");

        string newVars = sb.ToString();
        File.AppendAllText(envFilePath, newVars);

// COMPANY_API_TOKEN=4a26d8b7-584e-4f72-20d0-5855dddd8564
// COMPANY_API_CERT_PASSWORD=dev123
// COMPANY_API_PORT_INTERNAL=5032
// COMPANY_API_PORT_EXTERNAL=5031
// COMPANY_DB_CONNECTION_STRING="User ID=developer;Password=dev123;Host=company-db;Port=5438;Database=project_management_company"
// COMPANY_DB_PORT=5438
    }

    private void UpdatePrometheusLocalYaml(ServiceInitializationInfo info)
    {
        string ymlFilePath = _configurationService.PrometheusLocalFile;

        if (!File.Exists(ymlFilePath))
        {
            return;
        }

        string text = File.ReadAllText(ymlFilePath);
        
        IDeserializer deserializer = new DeserializerBuilder().Build();

        dynamic yamlObject = deserializer.Deserialize<dynamic>(new StringReader(text));
        List<dynamic>? scrapeConfigs = (List<dynamic>)yamlObject["scrape_configs"];

        Dictionary<string, object> newScrapeConfig = new()
        {
            { "job_name", info.ServiceNameKebabCaseAndApi },
            {
                "static_configs", new List<Dictionary<string, object>>
                {
                    new()
                    {
                        { "targets", new List<string> { $"host.docker.internal:{info.HttpsPort}" } },
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

    private void UpdateDockerComposeLocalYaml(ServiceInitializationInfo info)
    {
        string ymlFilePath = _configurationService.DockerComposeLocalFile;

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

        string volumeToAdd = $@"##VOLUMES_START##
  {info.DbHost}:
    driver: local";

        string networkToAdd = $@"##NETWORKS_START##
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

        string solutionFile = Path.Combine(_configurationService.GetBasePath(), $"{config.ProjectName}.sln");
        
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
