using System.Text.Json;
using Nexus.Config;

namespace Nexus.Services;

public class ConfigurationService
{
    private const string ConfigInitiated = "Nexus initiated";
    private const string ConfigAlreadyInitiated = "Nexus already initiated";
    private const string ServiceAdded = "Service added";
    private const string ServiceAlreadyExists = "Service already exists";

    public string GetBasePath() => @"C:\source\dotnet\nexus";
    // public string GetBasePath() => Directory.GetCurrentDirectory();
    
    public string GetConfigurationPath()
    {
        return Path.Combine(GetBasePath(), "nexus.config.json");
    }

    public string ApiGatewayOcelotDirectory => Path.Combine(GetBasePath(), @"\api-gateway\src\Nexus.ApiGateway\Ocelot");
    public string ApiGatewayConsulDirectory => Path.Combine(GetBasePath(), @"\api-gateway\src\Nexus.ApiGateway\Consul");
    public string HealthChecksDashboardConsulDirectory => Path.Combine(GetBasePath(), @"\health-checks-dashboard\src\Nexus.HealthChecksDashboard\Consul");
    public string GetServiceConsulDirectory(string projectName) => Path.Combine(GetBasePath(), "services", "src", projectName, "Consul");
    public string GetServiceAppSettingsFile(string projectName) => Path.Combine(GetBasePath(), "services", "src", projectName, "appsettings.json");
    
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
            ServiceName = info.ServiceNameKebabCase,
            ProjectName = info.ServiceNamePascalCasedAndDotApi,
            RootNamespace = info.RootNamespace,
            Port = info.HttpsPort,
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

    public string GetEnvironmentFile() => Path.Combine(GetBasePath(), ".env");
}