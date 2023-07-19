using Nexus.Config;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class InitializeDockerRunner : ComponentRunner
{
    public InitializeDockerRunner(ConfigurationService configurationService, RunType runType) : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        Console.WriteLine("Increasing WSL memory for ELK");
        RunPowershellCommand(@"wsl -d docker-desktop sysctl -w ""vm.max_map_count=262144""");
        
        Console.WriteLine("Checking Docker Networks");

        string networkList = RunDockerCommand($"network ls --filter \"name={state.NetworkName}\" --format \"{{{{.Name}}}}\"");
        
        if (networkList.Contains(state.NetworkName))
        {
            Console.WriteLine($"The network {state.NetworkName} already exists");
        }
        else
        {
            RunDockerCommand($"network create {state.NetworkName}");
            Console.WriteLine($"The network {state.NetworkName} has been created");
        }

        state.NetworkId = RunDockerCommand($"network inspect {state.NetworkName} \"--format={{{{.Id}}}}\"");
        state.SubnetIp = RunDockerCommand($"network inspect {state.NetworkId} \"--format={{{{(index .IPAM.Config 0).Subnet}}}}");
        
        Console.WriteLine($"Subnet IP: {state.SubnetIp}");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Docker Initializer";
}