using System.Text;
using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Models;
using Nexus.Services;
using Spectre.Console;

namespace Nexus.Runners;

public class ApiGatewayRunner : ServiceRunner<NexusServiceConfiguration>
{
    public ApiGatewayRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService, ProgressContext context)
        : base(configurationService, configuration, runType, consulApiService, context)
    {
    }

    private void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
    }

    protected override void UpdateAppConfig(RunState state)
    {
        string appConfigPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.ApiGatewayConsulDirectory,
            "app-config.json");

        if (!File.Exists(appConfigPath))
        {
            AddError($"File not found: app-config for {Configuration.ServiceName}", state);
            return;
        }

        string appConfigJson = File.ReadAllText(appConfigPath);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            AddError($"Unable to read file: app-config for {Configuration.ServiceName}", state);
            return;
        }

        ModifyAppConfig(appConfig, state);
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(appConfigPath, updatedAppConfigJson, Encoding.UTF8);

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
    }

    protected override void UpdateAppSettings(RunState state)
    {
        UpdateOcelotConfig(state);
        string appSettingsPath = Path.Combine(ConfigurationService.ApiGatewayAppSettingsFile);

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

    protected override PolicyCreationResult CreatePolicy(string globalToken)
    {
        string consulRulesFile = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.ApiGatewayConsulDirectory, "rules.hcl");

        if (!File.Exists(consulRulesFile))
        {
            return new PolicyCreationResult();
        }

        string rules = File.ReadAllText(consulRulesFile);

        PolicyCreationResult policy = ConsulApiService.CreateConsulPolicy(globalToken, rules, Configuration.ServiceName);
        return policy;
    }

    private void UpdateOcelotConfig(RunState state)
    {
        string ocelotConfigPath = Path.Combine(ConfigurationService.GetBasePath(), ConfigurationService.ApiGatewayOcelotDirectory,
            "ocelot.global.json");

        if (!File.Exists(ocelotConfigPath))
        {
            return;
        }

        string ocelotConfigJson = File.ReadAllText(ocelotConfigPath);
        dynamic? ocelotConfig = JsonConvert.DeserializeObject<dynamic>(ocelotConfigJson);

        if (ocelotConfig == null)
        {
            return;
        }

        ocelotConfig.GlobalConfiguration.ServiceDiscoveryProvider.Host = ConfigurationService.GetConsulHost(RunType);
        ocelotConfig.GlobalConfiguration.ServiceDiscoveryProvider.Token =
            state.ServiceTokens[Configuration.ServiceName];

        string updatedOcelotConfigJson = JsonConvert.SerializeObject(ocelotConfig, Formatting.Indented);
        File.WriteAllText(ocelotConfigPath, updatedOcelotConfigJson);
    }

    protected override string DisplayName => "API Gateway Runner";
}