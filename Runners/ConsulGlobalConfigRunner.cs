using System.Text;
using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Services;
using Spectre.Console;

namespace Nexus.Runners;

public class ConsulGlobalConfigRunner : ComponentRunner
{
    private readonly ConsulApiService _consulApiService;

    public ConsulGlobalConfigRunner(
        ConfigurationService configurationService,
        ConsulApiService consulApiService,
        RunType runType,
        ProgressContext context) : base(configurationService, runType, context)
    {
        _consulApiService = consulApiService;
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Updating Consul Global Config");
        if(!File.Exists(ConfigurationService.GlobalConsulFile))
        {
            AddError($"Unable to find nexus global config at {ConfigurationService.GlobalConsulFile}", state);
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        progressTask.Increment(25);

        string appConfigJson = File.ReadAllText(ConfigurationService.GlobalConsulFile);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            AddError($"Unable to parse nexus global config at {ConfigurationService.GlobalConsulFile}", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        progressTask.Increment(25);
 
        appConfig.SerilogSettings.ElasticSearchSettings.Uri = ConfigurationService.GetElasticSearchEndpoint(RunType);
        appConfig.TelemetrySettings.Endpoint = ConfigurationService.GetTelemetryEndpoint(RunType);
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(ConfigurationService.GlobalConsulFile, updatedAppConfigJson, Encoding.UTF8);
        
        _consulApiService.UploadKv("nexus-service", appConfigJson, state.GlobalToken);
        
        progressTask.Increment(50);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Consul Global Config Updater";
}