using Nexus.Config;
using Nexus.Services;
using Spectre.Console;

namespace Nexus.Runners.ApiGateway;

public abstract class ApiGatewayRunner : ServiceRunner<NexusServiceConfiguration>
{
    protected ApiGatewayRunner(
        ConfigurationService configurationService,
        NexusServiceConfiguration configuration, 
        RunType runType,
        ConsulApiService consulApiService, 
        ProgressContext context) 
        : base(configurationService, configuration, runType, consulApiService, context)
    {
    }
    
    protected override string DisplayName => "API Gateway Runner";
}