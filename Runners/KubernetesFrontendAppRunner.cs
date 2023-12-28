using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class KubernetesFrontendAppRunner : ComponentRunner
{
    public KubernetesFrontendAppRunner(ConfigurationService configurationService, RunType runType, ProgressContext context) 
        : base(configurationService, RunType.K8s, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask($"Setting up frontend app");
        string serviceFile = ConfigurationService.GetFrontendAppKubernetesServiceFile();
        
        if (!File.Exists(serviceFile))
        {
            AddError($"File not found: service.yaml for frontend-app", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        progressTask.Increment(20);
        
        RunPowershellCommand($"kubectl apply -f \"{serviceFile}\"");
        progressTask.Increment(100);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Frontend App Runner";
}