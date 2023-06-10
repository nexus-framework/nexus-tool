using System.Text;
using CaseExtensions;
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

    public bool InitializeSolution(string rawName)
    {
        // create solution file
        string solutionName = NameExtensions.GetKebabCasedNameWithoutApi(rawName);
        bool solutionCreated = CreateSolutionFile(solutionName);
        if (!solutionCreated)
        {
            return false;
        }
        
        // add config
        bool configInitialized = _configurationService.InitializeConfig(rawName);
        if (!configInitialized)
        {
            return false;
        }
        
        // ensure services folder
        EnsureServicesFolder();
        
        // add api gateway
        
        
        // add hc dashboard
        // add discovery server
        
        // Prerequisites:
        // * Library Packages to be published to nuget
        // * API Gateway/HealthCheck Dashboard published as templates/nuget packages
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
        ReplaceTemplateVariables(info);
        
        // Update docker compose yml
        // Add DB
        // Add networks
        UpdateDockerComposeLocalYaml(info);

        // Update prometheus yml
        UpdatePrometheusLocalYaml(info);

        UpdateEnvironmentFile(info);
        
        // Add service to solution
        AddServiceCsProjectFileToSolution(info.SolutionPath, info.ServiceCsProjectFile);
        return _configurationService.AddService(info);
    }

    private void UpdateEnvironmentFile(ServiceInitializationInfo info)
    {
        string envFilePath = _configurationService.EnvironmentFile;

        if (!File.Exists(envFilePath))
        {
            return;
        }

        StringBuilder sb = new StringBuilder();
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

        Dictionary<string, object> newScrapeConfig = new Dictionary<string, object>
        {
            { "job_name", info.ServiceNameKebabCaseAndApi },
            {
                "static_configs", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
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

    private bool CreateSolutionFile(string solutionName)
    {
        string slnFolder = _configurationService.GetBasePath(); 
        RunPowershellCommand($"dotnet new sln --output \"{slnFolder}\" --name {solutionName}");
        Console.WriteLine("Solution generated successfully!");
        return true;
    }

    private static bool AddServiceCsProjectFileToSolution(string solutionPath, string csProjectFilePath)
    {
        if (!File.Exists(csProjectFilePath))
        {
            // TODO: Write message
            return false;
        }

        RunPowershellCommand($"dotnet sln \"{solutionPath}\" add \"{csProjectFilePath}\"");
        return true;
    }
    
    private void EnsureServicesFolder()
    {
        string servicesFolderPath = Path.Combine(_configurationService.GetBasePath(), "services");
        if (!Directory.Exists(servicesFolderPath))
        {
            Directory.CreateDirectory(servicesFolderPath);
        }
    }
}
