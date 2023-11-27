using Nexus.Config;
using Spectre.Console;

namespace Nexus.Runners.DevCerts;

public abstract class DevCertsRunner : ComponentRunner
{
    protected DevCertsRunner(ConfigurationService configurationService, RunType runType, ProgressContext context) 
        : base(configurationService, runType, context)
    {
    }

    protected override string DisplayName => "Development Certificate Generator";
}