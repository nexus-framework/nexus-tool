using Newtonsoft.Json;
using Nexus.Core.Config;
using Nexus.Services;

namespace Nexus.Runners;

public class ApiGatewayRunner : ServiceRunner<ApiGatewayConfiguration>
{
    public ApiGatewayRunner(
        ConfigurationService configurationService,
        ApiGatewayConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService)
        : base(configurationService, configuration, runType, consulApiService)
    {
    }

    protected override void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
        appConfig.TelemetrySettings.Endpoint = "http://localhost:4317";
    }

    protected override void UpdateAppSettings(RunState state)
    {
        UpdateOcelotConfig(state);
        base.UpdateAppSettings(state);
    }

    private void UpdateOcelotConfig(RunState state)
    {
        string ocelotConfigPath = Path.Combine(ConfigurationService.GetBasePath(), Configuration.OcelotDirectory,
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

        ocelotConfig.GlobalConfiguration.ServiceDiscoveryProvider.Host = "localhost";
        ocelotConfig.GlobalConfiguration.ServiceDiscoveryProvider.Token =
            state.ServiceTokens[Configuration.ServiceName];

        string updatedOcelotConfigJson = JsonConvert.SerializeObject(ocelotConfig, Formatting.Indented);
        File.WriteAllText(ocelotConfigPath, updatedOcelotConfigJson);
    }
}
// public class ApiGatewayRunner : ComponentRunner
// {
//     private readonly ConsulApiService _consulApiService;
//     private readonly ApiGatewayConfiguration _configuration;
//
//     public ApiGatewayRunner(ConfigurationService configurationService,
//         RunType runType,
//         ConsulApiService consulApiService,
//         ApiGatewayConfiguration apiGatewayConfiguration
//         ) : base(configurationService, runType)
//     {
//         _consulApiService = consulApiService;
//         _configuration = apiGatewayConfiguration;
//     }
//
//     protected override RunState OnExecuted(RunState state)
//     {
//         // Create Policy
//         string consulRulesFile = Path.Combine(ConfigurationService.GetBasePath(), _configuration.ConsulConfigDirectory, "rules.hcl");
//
//         if (!File.Exists(consulRulesFile))
//         {
//             state.LastStepStatus = StepStatus.Failure;
//             return state;
//         }
//
//         string rules = File.ReadAllText(consulRulesFile);
//
//         PolicyCreationResult policy = _consulApiService.CreateConsulPolicy(state.GlobalToken, rules, _configuration.ServiceName);
//
//         if (!string.IsNullOrEmpty(policy.Id))
//         {
//             state.Policies[_configuration.ServiceName] = policy;
//         }
//         
//         // Create Token
//         string token = _consulApiService.CreateToken(state.GlobalToken, _configuration.ServiceName, policy.Name);
//         state.ServiceTokens.Add(_configuration.ServiceName, token);
//
//         UpdateAppConfig(state);
//         UpdateOcelotConfig(state);
//         UpdateAppSettings(state);
//
//         state.LastStepStatus = StepStatus.Success;
//         return state;
//     }
//
//     private void UpdateAppSettings(RunState state)
//     {
//         string appSettingsPath = Path.Combine(ConfigurationService.GetBasePath(), _configuration.AppSettingsConfigPath);
//
//         if (!File.Exists(appSettingsPath))
//         {
//             return;
//         }
//
//         string appSettingsJson = File.ReadAllText(appSettingsPath);
//         dynamic? appSettings = JsonConvert.DeserializeObject<dynamic>(appSettingsJson);
//
//         if (appSettings == null)
//         {
//             return;
//         }
//
//         appSettings.ConsulKV.Url = "http://localhost:8500";
//         appSettings.ConsulKV.Token = state.ServiceTokens[_configuration.ServiceName];
//         string updatedAppSettingsJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
//         File.WriteAllText(appSettingsPath, updatedAppSettingsJson);
//     }
//
//     private void UpdateOcelotConfig(RunState state)
//     {
//         string ocelotConfigPath = Path.Combine(ConfigurationService.GetBasePath(), _configuration.OcelotDirectory,
//             "ocelot.global.json");
//
//         if (!File.Exists(ocelotConfigPath))
//         {
//             return;
//         }
//
//         string ocelotConfigJson = File.ReadAllText(ocelotConfigPath);
//         dynamic? ocelotConfig = JsonConvert.DeserializeObject<dynamic>(ocelotConfigJson);
//
//         if (ocelotConfig == null)
//         {
//             return;
//         }
//
//         ocelotConfig.GlobalConfiguration.ServiceDiscoveryProvider.Host = "localhost";
//         ocelotConfig.GlobalConfiguration.ServiceDiscoveryProvider.Token = state.ServiceTokens[_configuration.ServiceName];
//         string updatedOcelotConfigJson = JsonConvert.SerializeObject(ocelotConfig, Formatting.Indented);
//         File.WriteAllText(ocelotConfigPath, updatedOcelotConfigJson);
//     }
//
//     private void UpdateAppConfig(RunState state)
//     {
//         string appConfigPath = Path.Combine(ConfigurationService.GetBasePath(), _configuration.ConsulConfigDirectory,
//             "app-config.json");
//
//         if (!File.Exists(appConfigPath))
//         {
//             return;
//         }
//
//         string appConfigJson = File.ReadAllText(appConfigPath);
//         dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);
//
//         if (appConfig == null)
//         {
//             return;
//         }
//
//         appConfig.Consul.Token = state.ServiceTokens[_configuration.ServiceName];
//         appConfig.TelemetrySettings.Endpoint = "http://localhost:4317";
//         string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
//         File.WriteAllText(appConfigPath, updatedAppConfigJson);
//
//         // Create KV
//         _consulApiService.UploadKv(_configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
//     }
// }