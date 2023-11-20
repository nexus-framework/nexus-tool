using Nexus.Config;
using Nexus.Runners;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Services;

public class CleanupService
{
    private readonly ConfigurationService _configurationService;

    public CleanupService(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }
    
    public bool Cleanup(RunType runType)
    {
        bool result = AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            })
            .Start(context => runType switch
                {
                    RunType.Local => CleanupDocker(context, runType),
                    RunType.Docker=> CleanupDocker(context, runType),
                    RunType.K8s => CleanupKubernetes(context),
                    _ => true,
                }
            );
        
        return result;
    }

    private bool CleanupDocker(ProgressContext context, RunType runType)
    {
        ProgressTask progressTask = context.AddTask("Cleaning up Docker Compose");
        string dockerComposePath = _configurationService.GetDockerComposePath(runType);
        string discoveryServerDockerComposePath = _configurationService.DiscoveryServerDockerCompose;

        string command = $"docker-compose -f \"{dockerComposePath}\" down -v";
        RunPowershellCommand(command);
        progressTask.Increment(100);
        progressTask.StopTask();

        progressTask = context.AddTask("Cleaning up Discovery Server");
        command = $"docker-compose -f \"{discoveryServerDockerComposePath}\" down -v";
        RunPowershellCommand(command);
        progressTask.Increment(100);
        progressTask.StopTask();
        return true;
    }
    
    private bool CleanupKubernetes(ProgressContext context)
    {
        ProgressTask progressTask = context.AddTask("Cleaning up Kubernetes");

        string consulYaml = _configurationService.KuberetesConsulFile;
        string prometheusYaml = _configurationService.KuberetesPrometheusFile;
        string elasticYaml = _configurationService.KubernetesElasticFile;
        string grafanaYaml = _configurationService.KubernetesGrafanaFile;

        RunPowershellCommand($"kubectl delete -f \"{consulYaml}\"");
        progressTask.Increment(25);
        
        RunPowershellCommand($"kubectl delete -f \"{prometheusYaml}\"");
        progressTask.Increment(25);
        
        RunPowershellCommand($"kubectl delete -f \"{elasticYaml}\"");
        progressTask.Increment(25);
        
        RunPowershellCommand($"kubectl delete -f \"{grafanaYaml}\"");
        progressTask.Increment(25);
        
        progressTask.StopTask();
        
        return true;
    }
}
