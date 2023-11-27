using Nexus.Config;
using Nexus.Extensions;
using Spectre.Console;

namespace Nexus.Runners.DevCerts;

public class DockerDevCertsRunner : DevCertsRunner
{
    public DockerDevCertsRunner(ConfigurationService configurationService, RunType runType, ProgressContext context) : base(configurationService, runType, context)
    {
    }
    
    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Generating Development Certificates");
        string output = ConsoleUtilities.RunPowershellCommand("dotnet dev-certs https -c");

        if (output.Contains("No valid certificate found."))
        {
            ConsoleUtilities.RunPowershellCommand("dotnet dev-certs https --trust");
        }

        string certPath = Path.Combine(ConfigurationService.GetBasePath(), "devcerts/aspnetapp.pfx");
        ConsoleUtilities.RunPowershellCommand($"dotnet dev-certs https -ep {certPath} -p {state.DevCertsPassword}");

        progressTask.Increment(100);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}