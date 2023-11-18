using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class DevCertsRunner : ComponentRunner
{
    public DevCertsRunner(ConfigurationService configurationService, RunType runType, ProgressContext context) 
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("[bold]Generating Development Certificates[/]");
        string output = RunPowershellCommand("dotnet dev-certs https -c");

        if (output.Contains("No valid certificate found."))
        {
            RunPowershellCommand("dotnet dev-certs https --trust");
        }

        string certPath = Path.Combine(ConfigurationService.GetBasePath(), "devcerts/aspnetapp.pfx");
        RunPowershellCommand($"dotnet dev-certs https -ep {certPath} -p {state.DevCertsPassword}");

        progressTask.Increment(100);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Development Certificate Generator";
}