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

namespace Nexus.Runners.StandardService;

public class KubernetesStandardServiceRunner : StandardServiceRunner
{
    public KubernetesStandardServiceRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        ConsulApiService consulApiService, ProgressContext context)
        : base(configurationService, configuration, RunType.K8s, consulApiService, context)
    {
    }

    protected override PolicyCreationResult CreatePolicy(RunState state)
    {
        string policyYaml = ConfigurationService.GetServiceKubernetesPolicyFile(Configuration.ServiceName);
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
        KubernetesClientConfiguration? config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
        Kubernetes client = new (config);
        
        try
        {
            HttpOperationResponse<V1Job> result;
            int retryCount = 0;
            do
            {
                Task.Delay(TimeSpan.FromSeconds(10)).Wait();
                result = client.BatchV1
                    .ReadNamespacedJobStatusWithHttpMessagesAsync($"apply-{Configuration.ServiceName}-policy", "nexus", true).GetAwaiter()
                    .GetResult();
            } while (result.Body.Status.Active is > 0 && retryCount++ < 10);
            
            if (result.Body.Status.Succeeded is > 0)
            {
                V1Secret? secret = client.ReadNamespacedSecret($"{Configuration.ServiceName}-token", "nexus", true);

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
        string appConfigPath = Path.Combine(
            ConfigurationService.GetBasePath(),
            ConfigurationService.GetServiceConsulDirectory(Configuration.ServiceName, Configuration.ProjectName),
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

        ModifyAppConfig(appConfig, state, Configuration.ServiceName);
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(appConfigPath, updatedAppConfigJson, Encoding.UTF8);

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
    }

    protected override void UpdateAppSettings(RunState state)
    {
        // Update appsettings.json
        string appSettingsPath = ConfigurationService.GetServiceAppSettingsFile(Configuration.ServiceName, Configuration.ProjectName);

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
        string serviceFile = ConfigurationService.GetServiceKubernetesServiceFile(Configuration.ServiceName);

        if (!File.Exists(serviceFile))
        {
            AddError($"File not found: service.yaml for {Configuration.ServiceName}", state);
            return;
        }

        RunPowershellCommand($"kubectl apply -f \"{serviceFile}\"");

        string dbExporterFile = ConfigurationService.GetServiceKubernetesDbExporterFile(Configuration.ServiceName);
        if (File.Exists(dbExporterFile))
        {
            RunPowershellCommand($"kubectl apply -f \"{dbExporterFile}\"");
        }
        
        base.RunService(state);
    }
    private void ModifyAppConfig(dynamic appConfig, RunState state, string serviceName)
    {
        appConfig.Postgres.Client.Host = ConfigurationService.GetDatabaseHost(RunType, serviceName);
        appConfig.Postgres.Client.Port = Configuration.DbPort ?? 5432;
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
    }
}