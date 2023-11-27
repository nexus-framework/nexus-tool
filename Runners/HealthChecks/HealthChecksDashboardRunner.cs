using Nexus.Config;
using Nexus.Services;
using Spectre.Console;

namespace Nexus.Runners.HealthChecks;

public abstract class HealthChecksDashboardRunner : ServiceRunner<NexusServiceConfiguration>
{
    protected HealthChecksDashboardRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService, ProgressContext context)
        : base(configurationService, configuration, runType, consulApiService, context)
    {
    }

    protected override string DisplayName => "Health Checks Dashboard Runner";
}