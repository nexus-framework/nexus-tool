using Nexus.Config;
using Nexus.Services;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

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

        RunType runType = RunType.Local;
        
        GlobalAppSettingsRunner globalAppSettingsRunner = new (_configurationService, runType);
        InitializeDockerRunner initializeDockerRunner = new (_configurationService, runType);
        DevCertsRunner devCertsRunner = new (_configurationService, runType);
        DiscoveryServerRunner discoveryServerRunner = new (_configurationService, runType);
        ApiGatewayRunner apiGatewayRunner = new (_configurationService, config.Framework.ApiGateway, runType, _consulApiService);
        HealthChecksDashboardRunner healthChecksDashboardRunner =
            new (_configurationService, config.Framework.HealthChecksDashboard, runType, _consulApiService);

        List<StandardServiceRunner> runners = new ();
        foreach (NexusServiceConfiguration? configuration in config.Services)
        {
            StandardServiceRunner? runner = new (_configurationService, configuration, runType, _consulApiService);
            runners.Add(runner);
        }

        for (int i = 0; i < runners.Count-1; i++)
        {
            runners[i].AddNextRunner(runners[i + 1]);
        }

        EnvironmentUpdateRunner environmentUpdateRunner = new (_configurationService, runType);
        DockerComposeRunner dockerComposeRunner = new (_configurationService, runType);
        
        runners.Last()
            .AddNextRunner(environmentUpdateRunner)
            .AddNextRunner(dockerComposeRunner);
        
        globalAppSettingsRunner
            .AddNextRunner(initializeDockerRunner)
            .AddNextRunner(devCertsRunner)
            .AddNextRunner(discoveryServerRunner)
            .AddNextRunner(apiGatewayRunner)
            .AddNextRunner(healthChecksDashboardRunner)
            .AddNextRunner(runners[0]);
        
        _state = globalAppSettingsRunner.Start(_state);

        if (_state.LastStepStatus == StepStatus.Success)
        {
            Console.WriteLine("Development Environment Setup Successfully");
            PrintState(_state);
            return 0;
        }
        
        Console.WriteLine("There were some errors setting up the development environment");
        return 1;
    }

    public int RunDocker()
    {
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            return 1;
        }

        RunType runType = RunType.Docker;
        
        GlobalAppSettingsRunner globalAppSettingsRunner = new (_configurationService, runType);
        InitializeDockerRunner initializeDockerRunner = new (_configurationService, runType);
        DevCertsRunner devCertsRunner = new (_configurationService, runType);
        BuildDockerImagesRunner buildDockerImagesRunner = new (_configurationService, runType);
        DiscoveryServerRunner discoveryServerRunner = new (_configurationService, runType);
        
        ApiGatewayRunner apiGatewayRunner = new (_configurationService, config.Framework.ApiGateway, runType, _consulApiService);
        HealthChecksDashboardRunner healthChecksDashboardRunner =
            new (_configurationService, config.Framework.HealthChecksDashboard, runType, _consulApiService);

        List<StandardServiceRunner> runners = new ();
        foreach (NexusServiceConfiguration? configuration in config.Services)
        {
            StandardServiceRunner runner = new (_configurationService, configuration, runType, _consulApiService);
            runners.Add(runner);
        }

        for (int i = 0; i < runners.Count-1; i++)
        {
            runners[i].AddNextRunner(runners[i + 1]);
        }

        EnvironmentUpdateRunner environmentUpdateRunner = new (_configurationService, runType);      
        DockerComposeRunner dockerComposeRunner = new (_configurationService, runType);

        runners.Last()
            .AddNextRunner(environmentUpdateRunner)
            .AddNextRunner(dockerComposeRunner);
        
        globalAppSettingsRunner
            .AddNextRunner(initializeDockerRunner)
            .AddNextRunner(devCertsRunner)
            .AddNextRunner(buildDockerImagesRunner)
            .AddNextRunner(discoveryServerRunner)
            .AddNextRunner(apiGatewayRunner)
            .AddNextRunner(healthChecksDashboardRunner)
            .AddNextRunner(runners[0]);
        
        _state = globalAppSettingsRunner.Start(_state);

        if (_state.LastStepStatus == StepStatus.Success)
        {
            Console.WriteLine("Development Environment Setup Successfully");
            PrintState(_state);
            return 0;
        }
        
        Console.WriteLine("There were some errors setting up the development environment");
        return 1;
    }
}