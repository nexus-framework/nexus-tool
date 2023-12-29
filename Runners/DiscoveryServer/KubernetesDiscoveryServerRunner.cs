using Nexus.Config;
using Nexus.Extensions;
using Spectre.Console;

namespace Nexus.Runners.DiscoveryServer;

public class KubernetesDiscoveryServerRunner : DiscoveryServerRunner
{
    public KubernetesDiscoveryServerRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Starting Discovery Server");
        AddLog("Starting Discovery Server", state);

        string consulYaml = ConfigurationService.KuberetesConsulFile;

        if (!File.Exists(consulYaml))
        {
            AddError($"File not found: {consulYaml}", state);
            AddLog($"File not found: {consulYaml}", state);
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        progressTask.Increment(10);

        ConsoleUtilities.RunPowershellCommand($"kubectl apply -f \"{consulYaml}\"");
        AddLog($"kubectl apply -f \"{consulYaml}\"", state);
        state.GlobalToken = "dev123";
        progressTask.Increment(10);
        
        // Wait for consul to be up
        AddLog("Waiting for consul pods to be ready", state);
        ConsoleUtilities.RunPowershellCommand($"kubectl wait --for=condition=ready pod -l app=consul -n nexus --timeout=300s");
        AddLog("Waiting complete", state);
        progressTask.Increment(10);
        Thread.Sleep(10 * 1000);
        
        progressTask.Increment(100);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}