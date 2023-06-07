using Nexus.Core.Config;
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

    protected override void ModifyAppConfig(dynamic appConfig, RunState state)
    {
        appConfig.SerilogSettings.ElasticSearchSettings.Uri = "https://localhost:9200";
        appConfig.Postgres.Client.Host = "localhost";
        appConfig.Consul.Token = state.ServiceTokens[Configuration.ServiceName];
        appConfig.TelemetrySettings.Endpoint = "http://localhost:4317";
    }
}