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
                ProgressTask t1 = context.AddTask("Checking solution directory");
                if (!_configurationService.IsSolutionDirectoryEmpty())
                {
                    AnsiConsole.MarkupLine("[red]Solution directory is not empty[/]");
                    t1.StopTask();
                    return false;
                }
                t1.Increment(100);
                t1.StopTask();
                
                // Download solution
                ProgressTask t2 = context.AddTask("Downloading solution template");
                string solutionName = NameExtensions.GetKebabCasedNameWithoutApi(rawName);
                string solutionDirectory = _configurationService.GetBasePath();
                bool downloadResult = await _gitHubService.DownloadSolutionTemplate(solutionName, solutionDirectory, cancellationToken);
                if (!downloadResult)
                {
                    AnsiConsole.MarkupLine("[red]Unable to download solution template[/]");
                    t2.StopTask();
                    return false;
                }
                t2.Increment(100);
                t2.StopTask();

                // Replace ProjectName in nexus config
                ProgressTask t3 = context.AddTask("Updating nexus config");
                NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

                if (config == null)
                {
                    AnsiConsole.MarkupLine("[red]Nexus config not found[/]");
                    t3.StopTask();
                    return false;
                }

                config.SolutionName = solutionName;
                _configurationService.WriteConfiguration(config);
                t3.Increment(100);
                t3.StopTask();

                // Replace names in docker-compose
                ProgressTask t4 = context.AddTask("Updating docker-compose");
                string dockerComposePath = _configurationService.GetDockerComposePath(RunType.Docker);

                if (!File.Exists(dockerComposePath))
                {
                    AnsiConsole.MarkupLine("[red]Unable to update docker-compose.yml[/]");
                    t4.StopTask();
                    return false;
                }

                string[] lines = await File.ReadAllLinesAsync(dockerComposePath, cancellationToken);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], @"image:\s+nexus.*:latest$"))
                    {
                        lines[i] = lines[i].Replace("nexus", solutionName);
                    }
                    
                    t4.Increment((double)1 / lines.Length * 100);
                }

                await File.WriteAllLinesAsync(dockerComposePath, lines, cancellationToken);
                t4.Increment(100);
                t4.StopTask();
                return true;
        });
        
        return result;
    }

    public async Task<bool> AddService(string rawName)
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
                    t1.StopTask();
                    return false;
                }
                t1.Increment(100);

                ProgressTask t2 = context.AddTask("Verifying service config");
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
                    AnsiConsole.MarkupLine($"[red]Service {info.ServiceNameKebabCaseAndApi} already exists[/]");
                    t2.StopTask();
                    return false;
                }
                t2.Increment(50);

                if (Directory.Exists(info.ServiceCsProjectFolder) && Directory.GetFiles(info.ServiceCsProjectFolder).Length > 0)
                {
                    AnsiConsole.MarkupLine($"[red]Folder {info.ServiceCsProjectFolder} is not empty[/]");
                    t2.StopTask();
                    return false;
                }
                t2.Increment(25);
                
                // Create service folders
                EnsureDirectories(new[] { info.ServiceRootFolder, info.ServiceCsProjectFolder });
                t2.Increment(25);

                // Download project template
                ProgressTask t3 = context.AddTask("Downloading project template");
                await _gitHubService.DownloadServiceTemplate(info.ServiceCsProjectFolder);
                t3.Increment(100);
                
                // Replace variables
                ProgressTask t4 = context.AddTask("Initializing project template");
                if (!ReplaceTemplateVariables(info, t4))
                {
                    t4.StopTask();
                    return false;
                }
                t4.Increment(100);
                
                // Update docker compose yml
                // Add DB
                // Add networks
                ProgressTask t5 = context.AddTask("Updating docker-compose");
                if (!UpdateDockerComposeYaml(info, RunType.Local))
                {
                    t5.StopTask();
                    return false;
                }
                t5.Increment(50);
                
                if(!UpdateDockerComposeYaml(info, RunType.Docker))
                {
                    t5.StopTask();
                    return false;
                }
                t5.Increment(50);

                // Update prometheus yml
                ProgressTask t6 = context.AddTask("Updating prometheus config");
                if(!UpdatePrometheusYaml(info, RunType.Local))
                {
                    t6.StopTask();
                    return false;
                }
                t6.Increment(50);

                if (!UpdatePrometheusYaml(info, RunType.Docker))
                {
                    t6.StopTask();
                    return false;
                }
                t6.Increment(50);
                
                ProgressTask t7 = context.AddTask("Updating environment file");
                if (!UpdateEnvironmentFile(info))
                {
                    t7.StopTask();
                    return false;
                }
                t7.Increment(100);
                
                // Add service to hc config
                ProgressTask t8 = context.AddTask("Updating health checks config");
                if (!UpdateHcConfig(info))
                {
                    t8.StopTask();
                    return false;
                }
                t8.Increment(100);
                
                // Add service to solution
                ProgressTask t9 = context.AddTask("Updating solution file");
                if (!AddCsProjectFileToSolution(info.SolutionPath, info.ServiceCsProjectFile))
                {
                    t9.StopTask();
                    return false;
                }
                t9.Increment(100);
                
                ProgressTask t10 = context.AddTask("Updating nexus config");
                if (!_configurationService.AddService(info))
                {
                    t10.StopTask();
                    return false;
                }
                t10.Increment(100);
                return true;
            });
        
        return result;
    }

    private bool UpdateHcConfig(ServiceInitializationInfo info)
    {
        string appConfigPath = Path.Combine(_configurationService.HealthChecksDashboardConsulDirectory, "app-config.json");

        if (!File.Exists(appConfigPath))
        {
            AnsiConsole.MarkupLine("[red]Nexus config not found[/]");
            return false;
        }

        string appConfigJson = File.ReadAllText(appConfigPath);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            AnsiConsole.MarkupLine("[red]Nexus config is unreadable[/]");
            return false;
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
        return true;
    }

    private bool UpdateEnvironmentFile(ServiceInitializationInfo info)
    {
        string envFilePath = _configurationService.EnvironmentFile;

        if (!File.Exists(envFilePath))
        {
            AnsiConsole.MarkupLine($"[red]Environment file not found[/] [cyan]{envFilePath}[/]");
            return false;
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
        return true;
    }

    private bool UpdatePrometheusYaml(ServiceInitializationInfo info, RunType runType)
    {
        string ymlFilePath = _configurationService.GetPrometheusFile(runType);

        if (!File.Exists(ymlFilePath))
        {
            AnsiConsole.MarkupLine($"[red]Prometheus config not found[/]");
            return false;
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
        return true;
    }

    private bool UpdateDockerComposeYaml(ServiceInitializationInfo info, RunType runType)
    {
        string ymlFilePath = _configurationService.GetDockerComposePath(runType);

        if (!File.Exists(ymlFilePath))
        {
            AnsiConsole.MarkupLine($"[red]docker-compose.yml not found[/]");
            return false;
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
        return true;
    }

    private bool ReplaceTemplateVariables(ServiceInitializationInfo info, ProgressTask progressTask)
    {
        // List files
        string[] files = Directory.GetFiles(info.ServiceCsProjectFolder, "*.*", SearchOption.AllDirectories);

        // Replace variables
        foreach (string file in files)
        {
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"[yellow]Unable to find file[/] [cyan]{file}[/]");
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
            progressTask.Increment((double)1/files.Length * 80);
        }
        
        // Rename csproj
        string? csProjFile = files.FirstOrDefault(x => x.EndsWith("ServiceTemplate.csproj"));

        if (csProjFile == null)
        {
            AnsiConsole.MarkupLine($"[yellow]Unable to find file[/] [cyan]ServiceTemplate.csproj[/]");
            return false;
        }

        string updatedName = csProjFile.Replace("ServiceTemplate.csproj", $"{info.ServiceNamePascalCasedAndDotApi}.csproj");
        File.Move(csProjFile, updatedName);
        progressTask.Increment(20);
        return true;
    }

    private static bool AddCsProjectFileToSolution(string solutionPath, string csProjectFilePath)
    {
        if (!File.Exists(csProjectFilePath))
        {
            AnsiConsole.MarkupLine($"[yellow]Unable to find file[/] [cyan]{csProjectFilePath}[/]");
            return false;
        }

        RunPowershellCommand($"dotnet sln \"{solutionPath}\" add \"{csProjectFilePath}\"");
        return true;
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
                    t1.StopTask();
                    return false;
                }
                t1.Increment(100);
                t1.StopTask();
        
                ProgressTask t2 = context.AddTask("Verifying libraries folder");
                string librariesFolder = Path.Combine(_configurationService.GetBasePath(), "libraries");
                if (Directory.Exists(librariesFolder))
                {
                    AnsiConsole.MarkupLine("[red]Libraries folder already exists[/]");
                    t2.StopTask();
                    return false;
                }
                t2.Increment(100);
                t2.StopTask();

                // Download libraries
                ProgressTask t3 = context.AddTask("Downloading libraries");
                string solutionFile = Path.Combine(_configurationService.GetBasePath(), $"{config.SolutionName}.sln");
                await _gitHubService.DownloadLibraries(librariesFolder);
                t3.Increment(100);
                t3.StopTask();
        
                // Clean up libraries folder
                ProgressTask t4 = context.AddTask("Cleaning up extra library files");
                CleanupLibrariesFolder(librariesFolder);
                t4.Increment(100);
                t4.StopTask();

                ProgressTask t5 = context.AddTask("Copying files to solution");
                // Add libraries to sln
                AddLibrariesToSolutionFile(librariesFolder, solutionFile, t5);
                t5.Increment(100);
                t5.StopTask();
        
                // Update csproj files
                ProgressTask t6 = context.AddTask("Updating csproj files");
                ReplaceLibrariesInCsProjFiles(librariesFolder, config, t6);
                t6.Increment(100);
                t6.StopTask();

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
