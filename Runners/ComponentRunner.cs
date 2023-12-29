using Nexus.Config;
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

    protected void AddLog(string message, RunState state)
    {
        state.Logs.Add($"{DateTime.Now:O}: {message}");
    }
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