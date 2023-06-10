using CaseExtensions;
using Nexus.Config;

namespace Nexus.Runners;

public class EnvironmentUpdateRunner : ComponentRunner
{
    public EnvironmentUpdateRunner(
        ConfigurationService configurationService,
        RunType runType) 
        : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        NexusSolutionConfiguration? config = ConfigurationService.ReadConfiguration();

        if (config == null)
        {
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

        string envFilePath = Path.Combine(ConfigurationService.GetBasePath(), ".env");

        if (!File.Exists(envFilePath))
        {
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

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

        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}