using System.Text.Json;
using Nexus.Core.Config;

namespace Nexus.Services;

public class ConfigurationService
{
    private const string CONFIG_INITIATED = "Nexus initiated";
    private const string CONFIG_ALREADY_INITIATED = "Nexus already initiated";
    private const string SERVICE_ADDED = "Service added";
    private const string SERVICE_ALREADY_EXISTS = "Service already exists";
    
    public string GetConfigurationPath()
    {
        string basePath = Directory.GetCurrentDirectory();
        return Path.Combine(basePath, "nexus.config.json");
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

        NexusSolutionConfiguration nexusSolutionConfig = new NexusSolutionConfiguration
        {
            ProjectName = name,
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
            Name = cleanedName,
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

        if (config.Services.Any(x => x.Name == serviceName))
        {
            Console.WriteLine(SERVICE_ALREADY_EXISTS);
            return true;
        }

        return false;
    }
}