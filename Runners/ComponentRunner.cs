using Nexus.Config;
using Nexus.Models;
using Spectre.Console;

namespace Nexus.Runners;

public abstract class ComponentRunner
{
    protected readonly RunType RunType;
    protected readonly ConfigurationService ConfigurationService;
    protected readonly ProgressContext Context;

    protected ComponentRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
    {
        ConfigurationService = configurationService;
        RunType = runType;
        Context = context;
    }
    
    private ComponentRunner? Next { get; set; }

    public ComponentRunner AddNextRunner(ComponentRunner nextRunner)
    {
        Next = nextRunner;
        return nextRunner;
    }
    
    public RunState Start(RunState state)
    {
        RunState updatedState = OnExecuted(state);

        if (state.LastStepStatus == StepStatus.Failure)
        {
            return updatedState;
        }

        if (Next == null)
        {
            return updatedState;
        }

        return Next.Start(updatedState);
    }
    
    protected abstract RunState OnExecuted(RunState state);

    protected abstract string DisplayName { get; }

    protected void AddError(string error, RunState state)
    {
        state.Errors.Add($"Error in {DisplayName}: {error}");
    }
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

    public List<string> Errors { get; set; } = new ();
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