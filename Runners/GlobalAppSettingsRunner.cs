using Newtonsoft.Json;
using Nexus.Config;
using Spectre.Console;

namespace Nexus.Runners;

public class GlobalAppSettingsRunner : ComponentRunner
{
    public GlobalAppSettingsRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Updating Global App Settings");
        string appSettingsPath = ConfigurationService.GlobalAppSettingsFile;
        if (!File.Exists(appSettingsPath))
        {
            AddError("File not found: appsettings.Global.json", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        progressTask.Increment(25);

        string appSettingsJson = File.ReadAllText(appSettingsPath);
        dynamic? appSettings = JsonConvert.DeserializeObject<dynamic>(appSettingsJson);

        if (appSettings == null)
        {
            AddError("Unable to read file: appsettings.Global.json", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        progressTask.Increment(25);
        appSettings.ConsulKV.Url = ConfigurationService.GetConsulEndpoint(RunType);

        string updatedAppSettingsJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        File.WriteAllText(appSettingsPath, updatedAppSettingsJson);

        progressTask.Increment(50);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Global App Settings Updater";
}