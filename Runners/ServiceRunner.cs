using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Models;
using Nexus.Services;

namespace Nexus.Runners;

public abstract class ServiceRunner<T> : ComponentRunner
    where T : NexusServiceConfiguration
{
    protected readonly T Configuration;

    protected readonly ConsulApiService ConsulApiService;
    
    protected ServiceRunner(
        ConfigurationService configurationService,
        T configuration,
        RunType runType, 
        ConsulApiService consulApiService) 
        : base(configurationService, runType)
    {
        Configuration = configuration;
        ConsulApiService = consulApiService;
    }

    protected override RunState OnExecuted(RunState state)
    {
        // Create policy
        Console.WriteLine($"Creating policy for {Configuration.ServiceName}");
        PolicyCreationResult policy = CreatePolicy(state.GlobalToken);

        if (string.IsNullOrEmpty(policy.Id))
        {
            Console.Error.WriteLine($"Unable to create policy for {Configuration.ServiceName}");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        
        Console.WriteLine($"Policy created for {Configuration.ServiceName}: {policy.Name} | {policy.Id}");
        state.Policies[Configuration.ServiceName] = policy;
        
        // Create token
        Console.WriteLine($"Creating token for {Configuration.ServiceName}");
        string serviceToken = CreateToken(state, policy.Name);

        if (string.IsNullOrEmpty(serviceToken))
        {
            Console.Error.WriteLine($"Unable to create service token. for {Configuration.ServiceName}");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        
        Console.WriteLine($"Created token for {Configuration.ServiceName}");
        state.ServiceTokens[Configuration.ServiceName] = serviceToken;
        
        // Update app-config
        // Create KV
        UpdateAppConfig(state);
        
        // Update AppSettings
        UpdateAppSettings(state);
        
        // Add to ServiceList
        state.ServiceUrls.Add(Configuration.ServiceName, $"https://localhost:{Configuration.Port}");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected virtual PolicyCreationResult CreatePolicy(string globalToken)
    {
        string consulRulesFile = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.GetServiceConsulDirectory(Configuration.ServiceName, Configuration.ProjectName), "rules.hcl");

        if (!File.Exists(consulRulesFile))
        {
            return new PolicyCreationResult();
        }

        string rules = File.ReadAllText(consulRulesFile);

        PolicyCreationResult policy = ConsulApiService.CreateConsulPolicy(globalToken, rules, Configuration.ServiceName);
        return policy;
    }

    protected virtual string CreateToken(RunState state, string policyName)
    {
        string token = ConsulApiService.CreateToken(state.GlobalToken, Configuration.ServiceName, policyName);
        return token;
    }
    
    protected virtual void UpdateAppConfig(RunState state)
    {
        Console.WriteLine($"Updating app-config for {Configuration.ServiceName}");
        string appConfigPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.GetServiceConsulDirectory(Configuration.ServiceName, Configuration.ProjectName),
            "app-config.json");

        if (!File.Exists(appConfigPath))
        {
            Console.Error.WriteLine($"File not found: app-config for {Configuration.ServiceName}");
            return;
        }

        string appConfigJson = File.ReadAllText(appConfigPath);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            Console.Error.WriteLine($"Unable to read file: app-config for {Configuration.ServiceName}");
            return;
        }
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(appConfigPath, updatedAppConfigJson);
        Console.WriteLine($"Updated app-config for {Configuration.ServiceName}");

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
        Console.WriteLine($"Pushed updated config for {Configuration.ServiceName} to Consul KV");
    }
    
    protected virtual void UpdateAppSettings(RunState state)
    {
        string appSettingsPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.GetServiceAppSettingsFile(Configuration.ServiceName, Configuration.ProjectName));

        if (!File.Exists(appSettingsPath))
        {
            Console.Error.WriteLine($"File not found: appsettings.json for {Configuration.ServiceName}");
            return;
        }

        string appSettingsJson = File.ReadAllText(appSettingsPath);
        dynamic? appSettings = JsonConvert.DeserializeObject<dynamic>(appSettingsJson);

        if (appSettings == null)
        {
            Console.Error.WriteLine($"Unable to read file: appsettings.json for {Configuration.ServiceName}");
            return;
        }
        
        appSettings.ConsulKV.Token = state.ServiceTokens[Configuration.ServiceName];
        
        string updatedAppSettingsJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        File.WriteAllText(appSettingsPath, updatedAppSettingsJson);
        
        Console.WriteLine($"Updated appsettings.json for {Configuration.ServiceName}");
    }
}