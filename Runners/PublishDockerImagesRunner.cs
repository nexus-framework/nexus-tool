using Nexus.Config;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class PublishDockerImagesRunner : ComponentRunner
{
    private readonly List<string> _defaultServices = new ()
    {
        "frontend-app",
        "api-gateway",
        "health-checks-dashboard",
    };
    public PublishDockerImagesRunner(ConfigurationService configurationService, RunType runType) : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        NexusSolutionConfiguration? config = ConfigurationService.ReadConfiguration();

        if (config == null)
        {
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        
        Console.WriteLine($"Publishing docker images for version {state.DockerImageVersion}");
        
        IEnumerable<string> allServices = _defaultServices.Concat(config.Services.Select(x => x.ServiceName));
        IEnumerable<string> imageNames = allServices.Select(x => $"{config.SolutionName}-{x}");
        IEnumerable<string> commands = imageNames.SelectMany(service => new string[]
        {
            $"push {config.DockerRepository}/{service}:{state.DockerImageVersion}",
            $"push {config.DockerRepository}/{service}:latest",
        });

        foreach (string command in commands)
        {
            RunDockerCommand(command, captureOutput: false);
        }
        
        Console.WriteLine($"Published docker images for version {state.DockerImageVersion}");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}