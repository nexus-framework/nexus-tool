using Nexus.Services;
using static Nexus.Services.ConsoleUtilities;

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
        string dockerComposePath = RunType switch
        {
            RunType.Local => Path.Combine(ConfigurationService.GetBasePath(), "docker-compose-local.yml"),
            RunType.Docker => Path.Combine(ConfigurationService.GetBasePath(), "docker-compose.yml"),
            _ => "",
        };
        
        string command = $"docker-compose -f \"{dockerComposePath}\" up -d";
        RunPowershellCommand(command, false);

        Console.WriteLine("Docker Services Started");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}