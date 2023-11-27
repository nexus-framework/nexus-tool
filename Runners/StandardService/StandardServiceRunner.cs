using Nexus.Config;
using Nexus.Services;
using Spectre.Console;

namespace Nexus.Runners.StandardService;

public abstract class StandardServiceRunner : ServiceRunner<NexusServiceConfiguration>
{
    public StandardServiceRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration,
        RunType runType,
        ConsulApiService consulApiService, ProgressContext context)
        : base(configurationService, configuration, runType, consulApiService, context)
    {
    }
    
    protected override string DisplayName => $"{Configuration.ServiceName} Runner";
}