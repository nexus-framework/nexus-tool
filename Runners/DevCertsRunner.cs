using Nexus.Services;
using static Nexus.Services.ConsoleUtilities;

namespace Nexus.Runners;

public class DevCertsRunner : ComponentRunner
{
    public DevCertsRunner(ConfigurationService configurationService, RunType runType) : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        string output = RunPowershellCommand("dotnet dev-certs https -c");

        if (output.Contains("No valid certificate found."))
        {
            RunPowershellCommand("dotnet dev-certs https --trust");
        }

        string certPath = Path.Combine(ConfigurationService.GetBasePath(), "devcerts/aspnetapp.pfx");
        RunPowershellCommand($"dotnet dev-certs https -ep {certPath} -p {state.DevCertsPassword}");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}