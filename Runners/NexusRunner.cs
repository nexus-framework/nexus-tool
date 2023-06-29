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

        InitializeDockerRunner initializeDockerRunner = new (_configurationService, RunType.Local);
        DevCertsRunner devCertsRunner = new (_configurationService, RunType.Local);
        DiscoveryServerRunner discoveryServerRunner = new (_configurationService, RunType.Local);
        ApiGatewayRunner apiGatewayRunner = new (_configurationService, config.Framework.ApiGateway, RunType.Local, _consulApiService);
        HealthChecksDashboardRunner healthChecksDashboardRunner =
            new (_configurationService, config.Framework.HealthChecksDashboard, RunType.Local, _consulApiService);

        List<StandardServiceRunner> runners = new ();
        foreach (NexusServiceConfiguration? configuration in config.Services)
        {
            StandardServiceRunner? runner = new (_configurationService, configuration, RunType.Local, _consulApiService);
            runners.Add(runner);
        }

        for (int i = 0; i < runners.Count-1; i++)
        {
            runners[i].AddNextRunner(runners[i + 1]);
        }

        EnvironmentUpdateRunner environmentUpdateRunner = new (_configurationService, RunType.Local);
        DockerComposeRunner dockerComposeRunner = new (_configurationService, RunType.Local);
        
        runners.Last()
            .AddNextRunner(environmentUpdateRunner)
            .AddNextRunner(dockerComposeRunner);
        
        initializeDockerRunner
            .AddNextRunner(devCertsRunner)
            .AddNextRunner(discoveryServerRunner)
            .AddNextRunner(apiGatewayRunner)
            .AddNextRunner(healthChecksDashboardRunner)
            .AddNextRunner(runners[0]);
        
        _state = initializeDockerRunner.Start(_state);

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

        InitializeDockerRunner initializeDockerRunner = new (_configurationService, RunType.Docker);
        DevCertsRunner devCertsRunner = new (_configurationService, RunType.Docker);
        BuildDockerImagesRunner buildDockerImagesRunner = new (_configurationService, RunType.Docker);
        DiscoveryServerRunner discoveryServerRunner = new (_configurationService, RunType.Docker);
        
        ApiGatewayRunner apiGatewayRunner = new (_configurationService, config.Framework.ApiGateway, RunType.Docker, _consulApiService);
        HealthChecksDashboardRunner healthChecksDashboardRunner =
            new (_configurationService, config.Framework.HealthChecksDashboard, RunType.Docker, _consulApiService);

        List<StandardServiceRunner> runners = new ();
        foreach (NexusServiceConfiguration? configuration in config.Services)
        {
            StandardServiceRunner? runner = new (_configurationService, configuration, RunType.Docker, _consulApiService);
            runners.Add(runner);
        }

        for (int i = 0; i < runners.Count-1; i++)
        {
            runners[i].AddNextRunner(runners[i + 1]);
        }

        EnvironmentUpdateRunner environmentUpdateRunner = new (_configurationService, RunType.Docker);
        DockerComposeRunner dockerComposeRunner = new (_configurationService, RunType.Docker);

        runners.Last()
            .AddNextRunner(environmentUpdateRunner)
            .AddNextRunner(dockerComposeRunner);
        
        initializeDockerRunner
            .AddNextRunner(devCertsRunner)
            .AddNextRunner(buildDockerImagesRunner)
            .AddNextRunner(discoveryServerRunner)
            .AddNextRunner(apiGatewayRunner)
            .AddNextRunner(healthChecksDashboardRunner)
            .AddNextRunner(runners[0]);
        
        _state = initializeDockerRunner.Start(_state);

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