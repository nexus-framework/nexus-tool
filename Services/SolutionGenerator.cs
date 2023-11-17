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
using Pastel;
using Spectre.Console;
using YamlDotNet.Serialization;
using static Nexus.Extensions.ConsoleUtilities;
using static Nexus.Extensions.DirectoryExtensions;

namespace Nexus.Services;

public class SolutionGenerator
{
    private readonly ConfigurationService _configurationService = new();
    private readonly GitHubService _gitHubService = new();

    public async Task<bool> InitializeSolution(string rawName, CancellationToken cancellationToken = default)
    {
        bool result = await AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async context =>
            {
                // Check solution directory
                ProgressTask t1 = context.AddTask("Checking solution directory", false, maxValue: 100D);
                if (!_configurationService.IsSolutionDirectoryEmpty())
                {
                    AnsiConsole.MarkupLine("[red]Solution directory is not empty[/]");
                    t1.Increment(100);
                    return false;
                }
                t1.Increment(100);
                
                // Download solution
                ProgressTask t2 = context.AddTask("Downloading solution template");
                string solutionName = NameExtensions.GetKebabCasedNameWithoutApi(rawName);
                string solutionDirectory = _configurationService.GetBasePath();
                bool downloadResult = await _gitHubService.DownloadSolutionTemplate(solutionName, solutionDirectory, cancellationToken);
                if (!downloadResult)
                {
                    AnsiConsole.MarkupLine("[red]Unable to download solution template[/]");
                    t2.Increment(100);
                    return false;
                }
                t2.Increment(100);

                // Replace ProjectName in nexus config
                ProgressTask t3 = context.AddTask("Updating nexus config");
                NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

                if (config == null)
                {
                    AnsiConsole.MarkupLine("[red]Nexus config not found[/]");
                    t3.Increment(100);
                    return false;
                }

                config.SolutionName = solutionName;
                _configurationService.WriteConfiguration(config);
                t3.Increment(100);

                // Replace names in docker-compose
                ProgressTask t4 = context.AddTask("Updating docker-compose");
                string dockerComposePath = _configurationService.GetDockerComposePath(RunType.Docker);

                if (!File.Exists(dockerComposePath))
                {
                    AnsiConsole.MarkupLine("[red]Unable to update docker-compose.yml[/]");
                    return false;
                }

                string[] lines = await File.ReadAllLinesAsync(dockerComposePath, cancellationToken);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], @"image:\s+nexus.*:latest$"))
                    {
                        lines[i] = lines[i].Replace("nexus", solutionName);
                    }
                }

                await File.WriteAllLinesAsync(dockerComposePath, lines, cancellationToken);
                t4.Increment(100);
                return true;
        });
        
        return result;
    }

    public async Task AddService(string rawName)
    {
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            //Console.WriteLine("Nexus config not found".Pastel(Constants.Colors.Error));
            return;
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
            //Console.WriteLine($"{"Service".Pastel(Constants.Colors.Error)} {info.ServiceNameKebabCaseAndApi.Pastel(Constants.Colors.Info)} {"already exists".Pastel(Constants.Colors.Error)}");
            return;
        }

        if (Directory.Exists(info.ServiceCsProjectFolder) && Directory.GetFiles(info.ServiceCsProjectFolder).Length > 0)
        {
            //Console.WriteLine($"{"Folder".Pastel(Constants.Colors.Error)} {info.ServiceCsProjectFolder.Pastel(Constants.Colors.Info)} {"is not empty".Pastel(Constants.Colors.Error)}");
            return;
        }
        
        // Create service folders
        EnsureDirectories(new[] { info.ServiceRootFolder, info.ServiceCsProjectFolder });

        // Download project template
        await _gitHubService.DownloadServiceTemplate(info.ServiceCsProjectFolder);
        
        // Replace variables
        //Console.WriteLine("Initializing project template");
        ReplaceTemplateVariables(info);
        
        // Update docker compose yml
        // Add DB
        // Add networks
        //Console.WriteLine("Updating docker-compose");
        UpdateDockerComposeYaml(info, RunType.Local);
        UpdateDockerComposeYaml(info, RunType.Docker);

        // Update prometheus yml
        //Console.WriteLine("Updating prometheus config");
        UpdatePrometheusYaml(info, RunType.Local);
        UpdatePrometheusYaml(info, RunType.Docker);

        //Console.WriteLine("Updating env file");
        UpdateEnvironmentFile(info);
        
        // Add service to hc config
        UpdateHcConfig(info);
        
        // Add service to solution
        AddCsProjectFileToSolution(info.SolutionPath, info.ServiceCsProjectFile);
        
        _configurationService.AddService(info);
    }

    private void UpdateHcConfig(ServiceInitializationInfo info)
    {
        //Console.WriteLine("Adding service to Health Checks");
        string appConfigPath = Path.Combine(_configurationService.HealthChecksDashboardConsulDirectory, "app-config.json");

        if (!File.Exists(appConfigPath))
        {
            //Console.WriteLine("Nexus config not found".Pastel(Constants.Colors.Error));
            return;
        }

        string appConfigJson = File.ReadAllText(appConfigPath);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            //Console.WriteLine("Nexus config is unreadable".Pastel(Constants.Colors.Error));
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
            //Console.WriteLine(".env not found".Pastel(Constants.Colors.Error));
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
            //Console.WriteLine("Prometheus config not found".Pastel(Constants.Colors.Error));
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
            //Console.WriteLine("docker-compose.yml not found".Pastel(Constants.Colors.Error));
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
                //Console.WriteLine($"{"Unable to find file".Pastel(Constants.Colors.Error)} {file.Pastel(Constants.Colors.Info)}");
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

        if (csProjFile == null)
        {
            //Console.WriteLine($"{"Unable to find file".Pastel(Constants.Colors.Error)} {"ServiceTemplate.csproj".Pastel(Constants.Colors.Info)}");
            return;
        }

        string updatedName = csProjFile.Replace("ServiceTemplate.csproj", $"{info.ServiceNamePascalCasedAndDotApi}.csproj");
        File.Move(csProjFile, updatedName);
    }

    private static void AddCsProjectFileToSolution(string solutionPath, string csProjectFilePath)
    {
        if (!File.Exists(csProjectFilePath))
        {
            //Console.WriteLine($"{"Unable to find".Pastel(Constants.Colors.Error)} {csProjectFilePath.Pastel(Constants.Colors.Info)}");
            return;
        }

        //Console.WriteLine($"Adding \"{csProjectFilePath}\" to the solution");
        RunPowershellCommand($"dotnet sln \"{solutionPath}\" add \"{csProjectFilePath}\"");
    }

    public async Task<bool> Eject()
    {
        bool result = await AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async context =>
            {
                ProgressTask t1 = context.AddTask("Checking config");
                NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();
                if (config == null)
                {
                    AnsiConsole.MarkupLine("[red]Nexus config not found[/]");
                    t1.Increment(100);
                    return false;
                }
                t1.Increment(100);
        
                ProgressTask t2 = context.AddTask("Verifying libraries folder");
                string librariesFolder = Path.Combine(_configurationService.GetBasePath(), "libraries");
                if (Directory.Exists(librariesFolder))
                {
                    AnsiConsole.MarkupLine("[red]Libraries folder already exists[/]");
                    t2.Increment(100);
                    return false;
                }
                t2.Increment(100);

                // Download libraries
                ProgressTask t3 = context.AddTask("Downloading libraries");
                string solutionFile = Path.Combine(_configurationService.GetBasePath(), $"{config.SolutionName}.sln");
                await _gitHubService.DownloadLibraries(librariesFolder);
                t3.Increment(100);
        
                // Clean up libraries folder
                ProgressTask t4 = context.AddTask("Cleaning up extra library files");
                CleanupLibrariesFolder(librariesFolder);
                t4.Increment(100);

                ProgressTask t5 = context.AddTask("Copying files to solution");
                // Add libraries to sln
                AddLibrariesToSolutionFile(librariesFolder, solutionFile, t5);
                t5.Increment(100);
        
                // Update csproj files
                ProgressTask t6 = context.AddTask("Updating csproj files");
                ReplaceLibrariesInCsProjFiles(librariesFolder, config, t6);
                t5.Increment(100);

                return true;
            });

        return result;
    }

    private void ReplaceLibrariesInCsProjFiles(string librariesFolder, NexusSolutionConfiguration config, ProgressTask progressTask)
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

        foreach (string csProjFile in csProjFilesToUpdate)
        {
            ReplaceLibrariesInCsProjFile(csProjFile, packages);
            progressTask.Increment((double)1 / csProjFilesToUpdate.Count * 100);
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
            }

            csProjDoc.Root?.Add(itemGroup);
        }
        
        csProjDoc.Save(csProjFilePath);
    }

    private void AddLibrariesToSolutionFile(string librariesFolder, string solutionPath, ProgressTask progressTask)
    {
        string srcPath = Path.Combine(librariesFolder, "src");
        string testsPath = Path.Combine(librariesFolder, "tests");

        string[] srcProjects = Directory.GetFiles(srcPath, "*.csproj", SearchOption.AllDirectories);
        string[] testProjects = Directory.GetFiles(testsPath, "*.csproj", SearchOption.AllDirectories);
        List<string> allProjects = srcProjects.Concat(testProjects).ToList();

        foreach (string project in allProjects)
        {
            AddCsProjectFileToSolution(solutionPath, project);
            progressTask.Increment((double)1 / allProjects.Count * 100);
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
