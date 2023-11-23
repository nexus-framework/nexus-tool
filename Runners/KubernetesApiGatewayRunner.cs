using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Models;
using Nexus.Services;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class KubernetesApiGatewayRunner : ApiGatewayRunner
{
    public KubernetesApiGatewayRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService,
        ProgressContext context) 
        : base(configurationService, configuration, runType, consulApiService, context)
    {
    }

    protected override PolicyCreationResult CreatePolicy(RunState state)
    {
        string policyYaml = ConfigurationService.ApiGatewayKubernetesPolicyFile;
        string policyName = $"kv-{Configuration.ServiceName}";
        
        if (!File.Exists(policyYaml))
        {
            AddError("Policy file not found", state);
            return PolicyCreationResult.Failure(policyName);
        }

        RunPowershellCommand($"kubectl apply -f \"{policyYaml}\"");
        return PolicyCreationResult.Success(policyName);
    }

    protected override string CreateToken(RunState state, string policyName)
    {
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
        var client = new Kubernetes(config);
        
        try
        {
            HttpOperationResponse<V1Job> result;
            int retryCount = 0;
            do
            {
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                result = client.BatchV1
                    .ReadNamespacedJobStatusWithHttpMessagesAsync("apply-api-gateway-policy", "nexus", true).GetAwaiter()
                    .GetResult();
                AnsiConsole.MarkupLine($"[red]retry {retryCount}[/]");
            } while (result.Body.Status.Active is > 0 && retryCount++ < 5);
            
            if (result.Body.Status.Succeeded is > 0)
            {
                V1Secret? secret = client.ReadNamespacedSecret("api-gateway-token", "nexus", true);

                if (secret.Data.TryGetValue("token", out byte[]? tokenBytes))
                {
                    string token = Encoding.Default.GetString(tokenBytes);
                    if (string.IsNullOrEmpty(token))
                    {
                        AddError("Unable to parse token", state);
                        return string.Empty;
                    }

                    return token;
                }
            }
        }
        catch (Exception ex)
        {
            AddError($"Unable to create service token {ex.Message}", state);
            return string.Empty;
        }
        
        return string.Empty;
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
        
        //Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
    }

    protected override void UpdateAppSettings(RunState state)
    {
        //Update Ocelot Config
        UpdateOcelotConfig(state);
        
        // Update appsettings.json
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

    protected override void RunService(RunState state)
    {
        // Start Service in K8s
        string serviceFile = ConfigurationService.ApiGatewayKubernetesServiceFile;

        if (!File.Exists(serviceFile))
        {
            AddError($"File not found: service.yaml for {Configuration.ServiceName}", state);
            return;
        }

        RunPowershellCommand($"kubectl apply -f \"{serviceFile}\"");
        base.RunService(state);
    }

    private void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
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
}