using System.Text.Json;
using Nexus.Models;

namespace Nexus.Config;

public class ConfigurationService
{
    private const string ConfigInitiated = "Nexus initiated";
    private const string ConfigAlreadyInitiated = "Nexus already initiated";
    private const string ServiceAdded = "Service added";
    private const string ServiceAlreadyExists = "Service already exists";

    // public string GetBasePath() => @"C:\source\dotnet\temp";
    public string GetBasePath() => Directory.GetCurrentDirectory();
    
    public string GetConfigurationPath()
    {
        return Path.Combine(GetBasePath(), "nexus.config.json");
    }

    public string ApiGatewayOcelotDirectory => Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway\Ocelot");
    public string ApiGatewayAppSettingsFile => Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway\appsettings.json");
    public string ApiGatewayConsulDirectory => Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway\Consul");
    public string HealthChecksDashboardConsulDirectory => Path.Combine(GetBasePath(), @"health-checks-dashboard\src\Nexus.HealthChecksDashboard\Consul");
    public string HealthChecksDashboardAppSettingsFile => Path.Combine(GetBasePath(), @"health-checks-dashboard\src\Nexus.HealthChecksDashboard\appsettings.json");

    public string GetServiceConsulDirectory(string serviceName, string projectName) =>
        Path.Combine(GetBasePath(), "services", serviceName, "src", projectName, "Consul");

    public string GetServiceAppSettingsFile(string serviceName, string projectName) =>
        Path.Combine(GetBasePath(), "services", serviceName, "src", projectName, "appsettings.json");
    
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

    public bool InitializeConfig(string name)
    {
        if (ConfigurationExists())
        {
            Console.WriteLine(ConfigAlreadyInitiated);
            return false;
        }

        NexusSolutionConfiguration nexusSolutionConfig = new()
        {
            ProjectName = name,
            Framework = new FrameworkConfiguration
            {
                ApiGateway = new ()
                {
                    Port = 7068,
                    ServiceName = "api-gateway",
                    ProjectName = "Nexus.ApiGateway",
                    RootNamespace = "Nexus.ApiGateway",
                },
                HealthChecksDashboard = new ()
                {
                    Port = 5051,
                    ServiceName = "health-checks-dashboard",
                    ProjectName = "Nexus.HealthChecksDashboard",
                    RootNamespace = "Nexus.HealthChecksDashboard",
                },
            },
            Services = new(),
        };

        WriteConfiguration(nexusSolutionConfig);

        Console.WriteLine(ConfigInitiated);
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
        Console.WriteLine(ServiceAdded);
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
            Console.WriteLine(ServiceAlreadyExists);
            return true;
        }

        return false;
    }

    public string EnvironmentFile => Path.Combine(GetBasePath(), ".env");
    public string DockerComposeLocalFile => Path.Combine(GetBasePath(), "docker-compose-local.yml");
    public string PrometheusLocalFile => Path.Combine(GetBasePath(), "prometheus-local.yml");

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