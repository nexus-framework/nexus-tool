using Nexus.Core.Config;
using Nexus.Runners;

namespace Nexus.Services;

internal class NexusRunner
{
    private readonly ConfigurationService _configurationService;
    private readonly ConsulApiService _consulApiService;
    private RunState _state;

    public NexusRunner(ConfigurationService configurationService, ConsulApiService consulApiService)
    {
        _configurationService = configurationService;
        _consulApiService = consulApiService;
        _state = new RunState(
            networkName: "consul_external",
            devCertsPassword: "dev123");
    }

    public int RunLocal()
    {
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            return 1;
        }

        InitializeDockerRunner initializeDockerRunner = new (_configurationService, RunType.Local);
        DevCertsRunner devCertsRunner = new (_configurationService, RunType.Local);
        DiscoveryServerRunner discoveryServerRunner = new (_configurationService, RunType.Local);
        ApiGatewayRunner apiGatewayRunner = new (_configurationService, RunType.Local, _consulApiService, config.Framework.ApiGateway);

        initializeDockerRunner
            .AddNextRunner(devCertsRunner)
            .AddNextRunner(discoveryServerRunner)
            .AddNextRunner(apiGatewayRunner);
        
        _state = initializeDockerRunner.Start(_state);
        return _state.LastStepStatus == StepStatus.Success ? 0 : 1;
    }

    public int RunDocker()
    {
        throw new NotImplementedException();
    }
}