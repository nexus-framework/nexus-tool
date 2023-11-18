using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Models;
using Nexus.Services;
using Spectre.Console;

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
        ConsulApiService consulApiService,
        ProgressContext context)
        : base(configurationService, runType, context)
    {
        Configuration = configuration;
        ConsulApiService = consulApiService;
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask($"Setting up {Configuration.ServiceName}");
        
        // Create policy
        PolicyCreationResult policy = CreatePolicy(state.GlobalToken);

        if (string.IsNullOrEmpty(policy.Id))
        {
            Console.Error.WriteLine($"Unable to create policy for {Configuration.ServiceName}");
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        
        progressTask.Increment(25);
        state.Policies[Configuration.ServiceName] = policy;
        
        // Create token
        string serviceToken = CreateToken(state, policy.Name);

        if (string.IsNullOrEmpty(serviceToken))
        {
            AnsiConsole.MarkupLine($"[red]Unable to create service token for {Configuration.ServiceName}[/]");
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        
        progressTask.Increment(25);
        state.ServiceTokens[Configuration.ServiceName] = serviceToken;
        
        // Update app-config
        // Create KV
        UpdateAppConfig(state);
        progressTask.Increment(25);
        
        // Update AppSettings
        UpdateAppSettings(state);
        progressTask.Increment(25);
        
        // Add to ServiceList
        state.ServiceUrls.Add(Configuration.ServiceName, $"https://localhost:{Configuration.Port}");

        progressTask.StopTask();
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
        string appConfigPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.GetServiceConsulDirectory(Configuration.ServiceName, Configuration.ProjectName),
            "app-config.json");

        if (!File.Exists(appConfigPath))
        {
            AnsiConsole.MarkupLine($"[red]File not found: app-config for {Configuration.ServiceName}[/]");
            return;
        }

        string appConfigJson = File.ReadAllText(appConfigPath);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            AnsiConsole.MarkupLine($"[red]Unable to read file: app-config for {Configuration.ServiceName}[/]");
            return;
        }
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(appConfigPath, updatedAppConfigJson);

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
    }
    
    protected virtual void UpdateAppSettings(RunState state)
    {
        string appSettingsPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.GetServiceAppSettingsFile(Configuration.ServiceName, Configuration.ProjectName));

        if (!File.Exists(appSettingsPath))
        {
            AnsiConsole.MarkupLine($"[red]File not found: appsettings.json for {Configuration.ServiceName}[/]");
            return;
        }

        string appSettingsJson = File.ReadAllText(appSettingsPath);
        dynamic? appSettings = JsonConvert.DeserializeObject<dynamic>(appSettingsJson);

        if (appSettings == null)
        {
            AnsiConsole.MarkupLine($"[red]Unable to read file: appsettings.json for {Configuration.ServiceName}[/]");
            return;
        }
        
        appSettings.ConsulKV.Token = state.ServiceTokens[Configuration.ServiceName];
        
        string updatedAppSettingsJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        File.WriteAllText(appSettingsPath, updatedAppSettingsJson);
    }
}