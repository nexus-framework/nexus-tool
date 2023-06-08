using Newtonsoft.Json;
using Nexus.Config;
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
        File.WriteAllText(appConfigPath, updatedAppConfigJson);
        Console.WriteLine($"Updated app-config for {Configuration.ServiceName}");

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
        Console.WriteLine($"Pushed upated config for {Configuration.ServiceName} to Consul KV");
    }

    private void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
        appConfig.SerilogSettings.ElasticSearchSettings.Uri = "https://localhost:9200";

        List<NexusServiceConfiguration>? services = ConfigurationService.ReadConfiguration()?.Services;

        if (services == null || appConfig.HealthCheck.Clients == null)
        {
            return;
        }

        foreach (NexusServiceConfiguration service in services!)
        {
            foreach (var client in appConfig.HealthCheck.Clients)
            {
                if (client.ServiceName == service.ServiceName)
                {
                    client.Url = $"https://localhost:{service.Port}/actuator/health";
                }
            }
        }
    }
}