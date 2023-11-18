using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class DockerComposeRunner : ComponentRunner
{
    public DockerComposeRunner(
        ConfigurationService configurationService,
        RunType runType, 
        ProgressContext context) : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Running Docker Compose");
        string dockerComposePath = ConfigurationService.GetDockerComposePath(RunType);
        string command = $"docker-compose -f \"{dockerComposePath}\" up -d";
        RunPowershellCommand(command);

        progressTask.Increment(100);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Docker Compose Runner";
}
