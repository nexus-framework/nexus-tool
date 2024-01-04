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
        progressTask.Increment(50);
        RunPowershellCommand($"kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=300s");
        AddLog("Prometheus is up", state);
        progressTask.Increment(100);
        progressTask.StopTask();
        
        // ElasticSearch
        string elasticYaml = ConfigurationService.KubernetesElasticFile;
        progressTask = Context.AddTask("Setting up ElasticSearch");
        RunPowershellCommand($"kubectl apply -f \"{elasticYaml}\"");
        progressTask.Increment(50);
        RunPowershellCommand($"kubectl wait --for=condition=ready pod -l app=elasticsearch -n monitoring --timeout=300s");
        AddLog("Elasticsearch is up", state);
        progressTask.Increment(100);
        progressTask.StopTask();

        // Grafana
        string grafanaYaml = ConfigurationService.KubernetesGrafanaFile;
        progressTask = Context.AddTask("Setting up Grafana");
        RunPowershellCommand($"kubectl apply -f \"{grafanaYaml}\"");
        progressTask.Increment(50);
        RunPowershellCommand($"kubectl wait --for=condition=ready pod -l app=grafana -n monitoring --timeout=300s");
        AddLog("Grafana is up", state);
        progressTask.Increment(100);
        progressTask.StopTask();
        
        // Jaeger
        string jaegerYaml = ConfigurationService.KubernetesJaegerFile;
        progressTask = Context.AddTask("Setting up Jaeger");
        RunPowershellCommand($"kubectl apply -f \"{jaegerYaml}\"");
        progressTask.Increment(50);
        RunPowershellCommand($"kubectl wait --for=condition=ready pod -l app=jaeger -n monitoring --timeout=300s");   
        AddLog("Jaeger is up", state);
        progressTask.Increment(100);
        progressTask.StopTask();
        
        Thread.Sleep(10 * 1000);

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}