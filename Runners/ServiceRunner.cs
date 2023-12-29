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
        AddLog($"Setting up {Configuration.ServiceName}", state);
        
        // Create policy
        PolicyCreationResult policyResult = CreatePolicy(state);

        if (policyResult.IsFailure())
        {
            AddError($"Unable to create policy for {Configuration.ServiceName}", state);
            AddLog($"Unable to create policy for {Configuration.ServiceName}", state);
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        
        progressTask.Increment(20);
        state.Policies[Configuration.ServiceName] = policyResult;
        
        // Create token
        string serviceToken = CreateToken(state, policyResult.Name);

        if (string.IsNullOrEmpty(serviceToken))
        {
            AddError($"Unable to create service token for {Configuration.ServiceName}", state);
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        
        progressTask.Increment(20);
        state.ServiceTokens[Configuration.ServiceName] = serviceToken;
        
        // Update app-config
        // Create KV
        UpdateAppConfig(state);
        progressTask.Increment(20);
        
        // Update AppSettings
        UpdateAppSettings(state);
        progressTask.Increment(20);
        
        RunService(state);
        progressTask.Increment(20);
        
        // Add to ServiceList
        state.ServiceUrls.Add(Configuration.ServiceName, $"https://localhost:{Configuration.Port}");

        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected virtual PolicyCreationResult CreatePolicy(RunState state)
    {
        AddLog("Creating policy", state);
        string consulRulesFile = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.GetServiceConsulDirectory(Configuration.ServiceName, Configuration.ProjectName), "rules.hcl");

        if (!File.Exists(consulRulesFile))
        {
            AddLog("Consul rules file not found", state);
            return new PolicyCreationResult();
        }

        string rules = File.ReadAllText(consulRulesFile);

        PolicyCreationResult policy = ConsulApiService.CreateConsulPolicy(state.GlobalToken, rules, Configuration.ServiceName);
        AddLog($"Policy created: {policy.Name}", state);
        return policy;
    }

    protected virtual string CreateToken(RunState state, string policyName)
    {
        AddLog($"Creating token for {Configuration.ServiceName}", state);
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
            AddError($"File not found: appsettings.json for {Configuration.ServiceName}", state);
            return;
        }

        string appSettingsJson = File.ReadAllText(appSettingsPath);
        dynamic? appSettings = JsonConvert.DeserializeObject<dynamic>(appSettingsJson);

        if (appSettings == null)
        {
            AddError($"Unable to read file: appsettings.json for {Configuration.ServiceName}", state);
            return;
        }
        
        appSettings.ConsulKV.Token = state.ServiceTokens[Configuration.ServiceName];
        
        string updatedAppSettingsJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        File.WriteAllText(appSettingsPath, updatedAppSettingsJson);
    }

    protected virtual void RunService(RunState state)
    {
        state.LastStepStatus = StepStatus.Success;
    }
}