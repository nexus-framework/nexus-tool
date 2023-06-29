using Nexus.Config;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class DockerComposeRunner : ComponentRunner
{
    public DockerComposeRunner(
        ConfigurationService configurationService,
        RunType runType) : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        string dockerComposePath = ConfigurationService.GetDockerComposePath(RunType);
        string command = $"docker-compose -f \"{dockerComposePath}\" up -d";
        RunPowershellCommand(command, false);

        Console.WriteLine("Docker Services Started");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}
