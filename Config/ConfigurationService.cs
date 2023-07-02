using System.Text.Json;
using Nexus.Extensions;
using Nexus.Models;
using Nexus.Runners;

namespace Nexus.Config;

public class ConfigurationService
{
    //public string GetBasePath() => @"C:\source\dotnet\temp";
    public string GetBasePath() => Directory.GetCurrentDirectory();
    
    public string GetConfigurationPath()
    {
        return Path.Combine(GetBasePath(), "nexus.config.json");
    }

    public string ApiGatewayDirectory => Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway");
    public string ApiGatewayDockerfile => Path.Combine(ApiGatewayDirectory, "Dockerfile");
    public string ApiGatewayOcelotDirectory => Path.Combine(ApiGatewayDirectory, "Ocelot");
    public string ApiGatewayAppSettingsFile => Path.Combine(ApiGatewayDirectory, "appsettings.json");
    public string ApiGatewayConsulDirectory => Path.Combine(ApiGatewayDirectory, "Consul");
    public string ApiGatewayCsProjFile => Path.Combine(ApiGatewayDirectory, "Nexus.ApiGateway.csproj");
    public string HealthChecksDashboardDirectory => Path.Combine(GetBasePath(), @"health-checks-dashboard\src\Nexus.HealthChecksDashboard");
    public string HealthChecksDashboardDockerfile => Path.Combine(HealthChecksDashboardDirectory, "Dockerfile");
    public string HealthChecksDashboardConsulDirectory => Path.Combine(HealthChecksDashboardDirectory, "Consul");
    public string HealthChecksDashboardAppSettingsFile => Path.Combine(HealthChecksDashboardDirectory, "appsettings.json");
    public string HealthChecksDashboardCsProjFile => Path.Combine(HealthChecksDashboardDirectory, "Nexus.HealthChecksDashboard.csproj");
    public string FrontEndAppDirectory => Path.Combine(GetBasePath(), @"frontend-app");

    public string DiscoveryServerConfigFolder => Path.Combine(GetBasePath(), @"discovery-server\docker\");
    public string DiscoveryServerDockerCompose => Path.Combine(DiscoveryServerConfigFolder, "docker-compose.yml");
    public string DiscoveryServerAcl => Path.Combine(DiscoveryServerConfigFolder, "consul-acl.json");

    public string GetServiceConsulDirectory(string serviceName, string projectName) =>
        Path.Combine(GetBasePath(), "services", serviceName, "src", projectName, "Consul");

    public string GetServiceAppSettingsFile(string serviceName, string projectName) =>
        Path.Combine(GetBasePath(), "services", serviceName, "src", projectName, "appsettings.json");
    
    public string GetServiceCsProjFile(string serviceName, string projectName) =>
        Path.Combine(GetBasePath(), "services", serviceName, "src", projectName, $"{projectName}.csproj");
    
    public string GetServiceDockerfile(string serviceName, string projectName) =>
        Path.Combine(GetBasePath(), "services", serviceName, "src", projectName, $"Dockerfile");

    public string GetDockerComposePath(RunType runType)
        => runType switch 
        {
            RunType.Local => Path.Combine(GetBasePath(), "docker-compose-local.yml"),
            RunType.Docker => Path.Combine(GetBasePath(), "docker-compose.yml"),
            _ => "",
        };

    public string GetTelemetryEndpoint(RunType runType) => runType switch
    {
        RunType.Local => "http://localhost:4317",
        RunType.Docker => "http://jaeger:4317",
        _ => "",
    };
    
    public string GetConsulEndpoint(RunType runType) => runType switch
    {
        RunType.Local => $"http://{GetConsulHost(runType)}:8500",
        RunType.Docker => "http://{GetConsulHost(runType)}:8500",
        _ => "",
    };
    
    public string GetConsulHost(RunType runType) => runType switch
    {
        RunType.Local => "localhost",
        RunType.Docker => "consul-server1",
        _ => "",
    };
    
    public string GetElasticSearchEndpoint(RunType runType)=> runType switch
    {
        RunType.Local => "https://localhost:9200",
        RunType.Docker => "https://es01:9200",
        _ => "",
    };

    public string GetDatabaseHost(RunType runType, string serviceName) => runType switch
    {
        RunType.Local => "localhost",
        RunType.Docker => $"{NameExtensions.GetKebabCasedNameWithoutApi(serviceName)}-db",
        _ => "",
    };

    public string GetPrometheusFile(RunType runType) => runType switch
    {
        RunType.Local => Path.Combine(GetBasePath(), "prometheus.local.yml"),
        RunType.Docker => Path.Combine(GetBasePath(), "prometheus.docker.yml"),
        _ => "",
    };
    
    public NexusSolutionConfiguration? ReadConfiguration()
    {
        if (!ConfigurationExists())
        {
            return null;
        }

        string json = File.ReadAllText(GetConfigurationPath());
        return JsonSerializer.Deserialize<NexusSolutionConfiguration>(json);
    }
    
    public void WriteConfiguration(NexusSolutionConfiguration solutionConfiguration)
    {
        string json = JsonSerializer.Serialize(solutionConfiguration, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        File.WriteAllText(GetConfigurationPath(), json);
    }
    
    public bool ConfigurationExists()
    {
        string configPath = GetConfigurationPath();

        if (!File.Exists(configPath))
        {
            return false;
        }

        return true;
    }

    public bool AddService(ServiceInitializationInfo info)
    {
        NexusSolutionConfiguration? config = ReadConfiguration();
        if (config == null)
        {
            return false;
        }
        
        config.Services.Add(new NexusServiceConfiguration
        {
            ServiceName = info.ServiceNameKebabCaseAndApi,
            ProjectName = info.ServiceNamePascalCasedAndDotApi,
            RootNamespace = info.RootNamespace,
            Port = info.HttpsPort,
            DbPort = info.DbPort,
        });

        WriteConfiguration(config);
        Console.WriteLine("Service added");
        return true;
    }

    public bool ServiceExists(string serviceName)
    {
        NexusSolutionConfiguration? config = ReadConfiguration();
        
        if (config == null)
        {
            return false;
        }

        if (config.Services.Any(x => x.ServiceName == serviceName))
        {
            Console.WriteLine("Service already exists");
            return true;
        }

        return false;
    }

    public string EnvironmentFile => Path.Combine(GetBasePath(), ".env");

    public int GetNewServicePort()
    {
        NexusSolutionConfiguration? config = ReadConfiguration();

        if (config == null)
        {
            return 0;
        }

        return config.Services.Select(x => x.Port).Max() + 2;
    }

    public int GetNewDbPort()
    {
        NexusSolutionConfiguration? config = ReadConfiguration();

        if (config == null)
        {
            return 0;
        }

        return config.Services.Where(x => x.DbPort.HasValue).Select(x => x.DbPort).Max() + 2 ?? 5438;
    }
}