using System.Text.Json;
using Nexus.Config;

namespace Nexus.Services;

public class ConfigurationService
{
    private const string CONFIG_INITIATED = "Nexus initiated";
    private const string CONFIG_ALREADY_INITIATED = "Nexus already initiated";
    private const string SERVICE_ADDED = "Service added";
    private const string SERVICE_ALREADY_EXISTS = "Service already exists";

    public string GetBasePath() => @"C:\source\dotnet\nexus";
    // public string GetBasePath() => Directory.GetCurrentDirectory();
    
    public string GetConfigurationPath()
    {
        return Path.Combine(GetBasePath(), "nexus.config.json");
    }
    
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
            Console.WriteLine(CONFIG_ALREADY_INITIATED);
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
                    OcelotDirectory = Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway\Ocelot"),
                    ConsulConfigDirectory = Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway\Consul"),
                    AppSettingsConfigPath = Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.ApiGateway\appsettings.json"),
                },
                HealthChecksDashboard = new ()
                {
                    Port = 5051,
                    ServiceName = "health-checks-dashboard",
                    ConsulConfigDirectory = Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.HealthChecksDashboard\Consul"),
                    AppSettingsConfigPath = Path.Combine(GetBasePath(), @"api-gateway\src\Nexus.HealthChecksDashboard\appsettings.json"),
                },
            },
            Services = new(),
        };

        WriteConfiguration(nexusSolutionConfig);

        Console.WriteLine(CONFIG_INITIATED);
        return true;
    }
    
    public bool AddService(string name)
    {
        string cleanedName = Utilities.GetKebabCasedNameAndApi(name);
        NexusSolutionConfiguration? config = ReadConfiguration();
        if (config == null)
        {
            return false;
        }
        
        config.Services.Add(new NexusServiceConfiguration
        {
            ServiceName = cleanedName,
        });

        WriteConfiguration(config);
        Console.WriteLine(SERVICE_ADDED);
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
            Console.WriteLine(SERVICE_ALREADY_EXISTS);
            return true;
        }

        return false;
    }
}