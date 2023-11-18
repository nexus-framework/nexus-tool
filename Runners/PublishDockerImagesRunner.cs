using Nexus.Config;
using Spectre.Console;
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
    public PublishDockerImagesRunner(
        ConfigurationService configurationService,
        RunType runType, 
        ProgressContext context) : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Publishing Docker Images");
        NexusSolutionConfiguration? config = ConfigurationService.ReadConfiguration();

        if (config == null)
        {
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            AddError("Nexus config not found", state);
            return state;
        }
        progressTask.Increment(10);
        
        IEnumerable<string> allServices = _defaultServices.Concat(config.Services.Select(x => x.ServiceName));
        List<string> imageNames = allServices.Select(x => $"{config.SolutionName}-{x}").ToList();

        Dictionary<string, string[]> serviceCommands = imageNames.Select(
            service =>
            {
                string[] sc = new string[]
                {
                    $"push {config.DockerRepository}/{service}:{state.DockerImageVersion}",
                    $"push {config.DockerRepository}/{service}:latest",
                };
                return new KeyValuePair<string, string[]>(service, sc);
            }
        ).ToDictionary(pair => pair.Key, pair => pair.Value);
        
        foreach (KeyValuePair<string, string[]> serviceCommand in serviceCommands)
        {
            progressTask.Description($"Publishing Docker Images: {serviceCommand.Key}");
            foreach (string command in serviceCommand.Value)
            {
                RunDockerCommand(command);
            }
            progressTask.Increment((double)1/serviceCommands.Count * 80);
        }
        
        state.LastStepStatus = StepStatus.Success;
        progressTask.Increment(100);
        progressTask.StopTask();
        return state;
    }

    protected override string DisplayName => "Docker Images Publisher";
}