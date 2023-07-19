using Newtonsoft.Json;
using Nexus.Config;

namespace Nexus.Runners;

public class GlobalAppSettingsRunner : ComponentRunner
{
    public GlobalAppSettingsRunner(ConfigurationService configurationService, RunType runType)
        : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        string appSettingsPath = ConfigurationService.GlobalAppSettingsFile;

        if (!File.Exists(appSettingsPath))
        {
            Console.Error.WriteLine($"File not found: appsettings.Global.json");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

        string appSettingsJson = File.ReadAllText(appSettingsPath);
        dynamic? appSettings = JsonConvert.DeserializeObject<dynamic>(appSettingsJson);

        if (appSettings == null)
        {
            Console.Error.WriteLine($"Unable to read file: appsettings.Global.json");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

        appSettings.ConsulKV.Url = ConfigurationService.GetConsulEndpoint(RunType);

        string updatedAppSettingsJson = JsonConvert.SerializeObject(appSettings, Formatting.Indented);
        File.WriteAllText(appSettingsPath, updatedAppSettingsJson);

        Console.WriteLine($"Updated appsettings.Global.json");

        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Global App Settings Updater";
}