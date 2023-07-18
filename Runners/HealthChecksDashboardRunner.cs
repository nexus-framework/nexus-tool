﻿using System.Text;
using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Models;
using Nexus.Services;

namespace Nexus.Runners;

public class HealthChecksDashboardRunner : ServiceRunner<NexusServiceConfiguration>
{
    public HealthChecksDashboardRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService)
        : base(configurationService, configuration, runType, consulApiService)
    {
    }

    protected override void UpdateAppConfig(RunState state)
    {
        Console.WriteLine($"Updating app-config for {Configuration.ServiceName}");
        string appConfigPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.HealthChecksDashboardConsulDirectory,
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

        ModifyAppConfig(appConfig, state);
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(appConfigPath, updatedAppConfigJson, Encoding.UTF8);
        Console.WriteLine($"Updated app-config for {Configuration.ServiceName}");

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
        Console.WriteLine($"Pushed updated config for {Configuration.ServiceName} to Consul KV");
    }

    protected override void UpdateAppSettings(RunState state)
    {
        string appSettingsPath = Path.Combine(ConfigurationService.HealthChecksDashboardAppSettingsFile);

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
    
    protected override PolicyCreationResult CreatePolicy(string globalToken)
    {
        string consulRulesFile = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.HealthChecksDashboardConsulDirectory, "rules.hcl");

        if (!File.Exists(consulRulesFile))
        {
            return new PolicyCreationResult();
        }

        string rules = File.ReadAllText(consulRulesFile);

        PolicyCreationResult policy = ConsulApiService.CreateConsulPolicy(globalToken, rules, Configuration.ServiceName);
        return policy;
    }

    private void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
    }
}