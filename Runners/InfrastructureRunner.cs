using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public abstract class InfrastructureRunner : ComponentRunner
{
    protected InfrastructureRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }


    protected override string DisplayName => "Infrastructure Runner";
}

public class KubernetesInfrastructureRunner : InfrastructureRunner
{
    public KubernetesInfrastructureRunner(ConfigurationService configurationService, ProgressContext context) 
        : base(configurationService, RunType.K8s, context)
    {
    }
    
    protected override RunState OnExecuted(RunState state)
    {
        // Prometheus
        ProgressTask progressTask = Context.AddTask("Setting up Prometheus");
        string prometheusYaml = ConfigurationService.KuberetesPrometheusFile;
        RunPowershellCommand($"kubectl apply -f \"{prometheusYaml}\"");
        progressTask.Increment(100);
        progressTask.StopTask();
        
        // ElasticSearch
        string elasticYaml = ConfigurationService.KubernetesElasticFile;
        progressTask = Context.AddTask("Setting up ElasticSearch");
        RunPowershellCommand($"kubectl apply -f \"{elasticYaml}\"");
        progressTask.Increment(100);
        progressTask.StopTask();

        // Grafana
        string grafanaYaml = ConfigurationService.KubernetesGrafanaFile;
        progressTask = Context.AddTask("Setting up Grafana");
        RunPowershellCommand($"kubectl apply -f \"{grafanaYaml}\"");
        progressTask.Increment(100);
        progressTask.StopTask();
        
        // Jaeger
        string jaegerYaml = ConfigurationService.KubernetesJaegerFile;
        progressTask = Context.AddTask("Setting up Jaeger");
        RunPowershellCommand($"kubectl apply -f \"{jaegerYaml}\"");
        progressTask.Increment(100);
        progressTask.StopTask();

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}