using System.Text;
using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Services;
using Spectre.Console;

namespace Nexus.Runners;

public class StandardServiceRunner : ServiceRunner<NexusServiceConfiguration>
{
    public StandardServiceRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService, ProgressContext context)
        : base(configurationService, configuration, runType, consulApiService, context)
    {
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

    private void ModifyAppConfig(dynamic appConfig, RunState state, string serviceName)
    {
        appConfig.Postgres.Client.Host = ConfigurationService.GetDatabaseHost(RunType, serviceName);
        appConfig.Postgres.Client.Port = Configuration.DbPort ?? 5432;
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
    }

    protected override string DisplayName => $"{Configuration.ServiceName} Runner";
}