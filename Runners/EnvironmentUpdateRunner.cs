using CaseExtensions;
using Nexus.Config;
using Spectre.Console;

namespace Nexus.Runners;

public class EnvironmentUpdateRunner : ComponentRunner
{
    public EnvironmentUpdateRunner(
        ConfigurationService configurationService,
        RunType runType,
        ProgressContext context) 
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Updating environment variables");
        NexusSolutionConfiguration? config = ConfigurationService.ReadConfiguration();

        if (config == null)
        {
            AddError("Unable to read configuration", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        progressTask.Increment(25);

        string envFilePath = Path.Combine(ConfigurationService.GetBasePath(), ".env");

        if (!File.Exists(envFilePath))
        {
            AddError("Unable to find .env file", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        progressTask.Increment(25);

        string[] lines = File.ReadAllLines(envFilePath);
        
        foreach (KeyValuePair<string, string> serviceToken in state.ServiceTokens)
        {
            string? envVar = $"{serviceToken.Key.ToSnakeCase().ToUpperInvariant()}_TOKEN";
            for(int i =0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(envVar))
                {
                    lines[i] = $"{envVar}={serviceToken.Value}";
                    break;
                }
            }
        }
        File.WriteAllLines(envFilePath, lines);
        
        progressTask.Increment(50);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Environment Updater";
}