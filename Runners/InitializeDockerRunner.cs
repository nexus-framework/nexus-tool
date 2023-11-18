using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class InitializeDockerRunner : ComponentRunner
{
    public InitializeDockerRunner(ConfigurationService configurationService, RunType runType, ProgressContext context) 
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask($"Initializing Docker");
        RunPowershellCommand(@"wsl -d docker-desktop sysctl -w ""vm.max_map_count=262144""");
        progressTask.Increment(25);
        
        string networkList = RunDockerCommand($"network ls --filter \"name={state.NetworkName}\" --format \"{{{{.Name}}}}\"");

        if (!networkList.Contains(state.NetworkName))
        {
            RunDockerCommand($"network create {state.NetworkName}");
        }
        progressTask.Increment(25);

        state.NetworkId = RunDockerCommand($"network inspect {state.NetworkName} \"--format={{{{.Id}}}}\"");
        state.SubnetIp = RunDockerCommand($"network inspect {state.NetworkId} \"--format={{{{(index .IPAM.Config 0).Subnet}}}}");
        
        progressTask.Increment(50);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Docker Initializer";
}