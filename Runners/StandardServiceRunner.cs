using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Services;

namespace Nexus.Runners;

public class StandardServiceRunner : ServiceRunner<NexusServiceConfiguration>
{
    public StandardServiceRunner(
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

        ModifyAppConfig(appConfig, state);
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(appConfigPath, updatedAppConfigJson);
        Console.WriteLine($"Updated app-config for {Configuration.ServiceName}");

        // Create KV
        ConsulApiService.UploadKv(Configuration.ServiceName, updatedAppConfigJson, state.GlobalToken);
        Console.WriteLine($"Pushed updated config for {Configuration.ServiceName} to Consul KV");
    }

    private void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.SerilogSettings.ElasticSearchSettings.Uri = "https://localhost:9200";
        appConfig.Postgres.Client.Host = "localhost";
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
        appConfig.TelemetrySettings.Endpoint = "http://localhost:4317";
    }
}