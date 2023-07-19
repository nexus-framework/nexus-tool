using System.Drawing;
using Colorful;
using Nexus.Config;
using Nexus.Models;
using Console = Colorful.Console;

namespace Nexus.Runners;

public abstract class ComponentRunner
{
    protected readonly RunType RunType;
    protected readonly ConfigurationService ConfigurationService;

    protected ComponentRunner(ConfigurationService configurationService, RunType runType)
    {
        ConfigurationService = configurationService;
        RunType = runType;
    }
    
    private ComponentRunner? Next { get; set; }

    public ComponentRunner AddNextRunner(ComponentRunner nextRunner)
    {
        Next = nextRunner;
        return nextRunner;
    }
    
    public RunState Start(RunState state)
    {
        StyleSheet ss = new StyleSheet(Color.Black);
        ss.AddStyle(DisplayName, Color.Cyan);
        Console.WriteLineStyled($"Starting {DisplayName}", ss);
        RunState updatedState = OnExecuted(state);

        if (state.LastStepStatus == StepStatus.Failure)
        {
            ss.UnstyledColor = Color.Red;
            Console.WriteLineStyled($"{DisplayName} failed. Aborting", ss);
            return updatedState;
        }

        if (Next == null)
        {
            return updatedState;
        }

        Console.WriteLine("**************************************************\n\n");
        return Next.Start(updatedState);
    }
    
    protected abstract RunState OnExecuted(RunState state);

    protected abstract string DisplayName { get; }
}

public class RunState
{
    public RunState(string networkName, string devCertsPassword)
    {
        NetworkName = networkName;
        DevCertsPassword = devCertsPassword;
    }
    
    public string NetworkName { get; set; }
    public string NetworkId { get; set; } = string.Empty;
    public string SubnetIp { get; set; } = string.Empty;
    public string GlobalToken { get; set; } = string.Empty;
    public string DevCertsPassword { get; set; }
    
    public Dictionary<string, PolicyCreationResult> Policies = new ();
    
    public StepStatus LastStepStatus { get; set; }

    public Dictionary<string, string> ServiceUrls { get; set; } = new ();

    public string DockerImageVersion { get; set; } = string.Empty;

    public Dictionary<string, string> ServiceTokens = new ();

}

public enum StepStatus
{
    Success,
    Failure,
}

public enum RunType
{
    Local = 0,
    Docker = 1,
    K8s = 2,
}