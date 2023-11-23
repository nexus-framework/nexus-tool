using Nexus.Config;
using Spectre.Console;

namespace Nexus.Runners;

public abstract class DiscoveryServerRunner : ComponentRunner
{
    protected DiscoveryServerRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }

    protected override string DisplayName => "Discovery Service Runner";
}