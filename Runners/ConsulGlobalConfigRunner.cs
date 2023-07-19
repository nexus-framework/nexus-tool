using System.Text;
using Newtonsoft.Json;
using Nexus.Config;
using Nexus.Services;

namespace Nexus.Runners;

public class ConsulGlobalConfigRunner : ComponentRunner
{
    private readonly ConsulApiService _consulApiService;

    public ConsulGlobalConfigRunner(
        ConfigurationService configurationService,
        ConsulApiService consulApiService,
        RunType runType) : base(configurationService, runType)
    {
        _consulApiService = consulApiService;
    }

    protected override RunState OnExecuted(RunState state)
    {        
        if(!File.Exists(ConfigurationService.GlobalConsulFile))
        {
            Console.Error.WriteLine("Unable to add nexus global config to consul");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

        string appConfigJson = File.ReadAllText(ConfigurationService.GlobalConsulFile);
        dynamic? appConfig = JsonConvert.DeserializeObject<dynamic>(appConfigJson);

        if (appConfig == null)
        {
            Console.Error.WriteLine($"Unable to consul global config");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
 
        appConfig.SerilogSettings.ElasticSearchSettings.Uri = ConfigurationService.GetElasticSearchEndpoint(RunType);
        appConfig.TelemetrySettings.Endpoint = ConfigurationService.GetTelemetryEndpoint(RunType);
        
        string updatedAppConfigJson = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
        File.WriteAllText(ConfigurationService.GlobalConsulFile, updatedAppConfigJson, Encoding.UTF8);
        Console.WriteLine($"Updated consul global config");
        
        _consulApiService.UploadKv("nexus-service", appConfigJson, state.GlobalToken);
        Console.WriteLine($"Pushed updated consul global config to Consul KV");
        
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Consul Global Config Updater";
}