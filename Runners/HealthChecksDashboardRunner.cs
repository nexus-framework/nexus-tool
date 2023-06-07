using Newtonsoft.Json;
using Nexus.Core.Config;
using Nexus.Services;

namespace Nexus.Runners;

public class HealthChecksDashboardRunner : ServiceRunner<HealthChecksDashboardConfiguration>
{
    public HealthChecksDashboardRunner(
        ConfigurationService configurationService,
        HealthChecksDashboardConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService)
        : base(configurationService, configuration, runType, consulApiService)
    {
    }

    protected override void ModifyAppConfig(dynamic appConfig, RunState state)
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