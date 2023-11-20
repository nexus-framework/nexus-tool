using Nexus.Config;
using Nexus.Services;
using Spectre.Console;
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

    public bool RunLocal()
    {
        bool result = AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            })
            .Start(context =>
            {
                NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();
                if (config == null)
                {
                    AnsiConsole.MarkupLine("[red]Unable to read configuration[/]");
                    return false;
                }

                RunType runType = RunType.Local;
                
                GlobalAppSettingsRunner globalAppSettingsRunner = new (_configurationService, runType, context);
                InitializeDockerRunner initializeDockerRunner = new (_configurationService, runType, context);
                DevCertsRunner devCertsRunner = new (_configurationService, runType, context);
                DockerDiscoveryServerRunner dockerDiscoveryServerRunner = new (_configurationService, runType, context);
                ApiGatewayRunner apiGatewayRunner = new (_configurationService, config.Framework.ApiGateway, runType, _consulApiService, context);
                HealthChecksDashboardRunner healthChecksDashboardRunner =
                    new (_configurationService, config.Framework.HealthChecksDashboard, runType, _consulApiService, context);

                List<StandardServiceRunner> runners = new ();
                foreach (NexusServiceConfiguration? configuration in config.Services)
                {
                    StandardServiceRunner? runner = new (_configurationService, configuration, runType, _consulApiService, context);
                    runners.Add(runner);
                }

                for (int i = 0; i < runners.Count-1; i++)
                {
                    runners[i].AddNextRunner(runners[i + 1]);
                }

                ConsulGlobalConfigRunner consulGlobalConfigRunner = new (_configurationService, _consulApiService, runType, context);
                EnvironmentUpdateRunner environmentUpdateRunner = new (_configurationService, runType, context);
                DockerComposeRunner dockerComposeRunner = new (_configurationService, runType, context);
                
                runners.Last()
                    .AddNextRunner(consulGlobalConfigRunner)
                    .AddNextRunner(environmentUpdateRunner)
                    .AddNextRunner(dockerComposeRunner);
                
                globalAppSettingsRunner
                    .AddNextRunner(initializeDockerRunner)
                    .AddNextRunner(devCertsRunner)
                    .AddNextRunner(dockerDiscoveryServerRunner)
                    .AddNextRunner(apiGatewayRunner)
                    .AddNextRunner(healthChecksDashboardRunner)
                    .AddNextRunner(runners[0]);
                
                _state = globalAppSettingsRunner.Start(_state);

                if (_state.LastStepStatus == StepStatus.Success)
                {
                    return true;
                }
                
                return false;
            });

        if (result)
        {
            AnsiConsole.MarkupLine("[green]Development Environment Setup Successfully[/]");
            PrintState(_state);
            PrintVersion(_state);
            return true;
        }
        
        foreach (string error in _state.Errors)
        {
            AnsiConsole.MarkupLine($"[red]{error}[/]");
        }
        return false;
    }

    public bool RunDocker()
    {
        bool result = AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn()
                {
                    Alignment = Justify.Left,
                },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(Spinner.Known.Dots),
            })
            .Start(context =>
            {
                NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

                if (config == null)
                {
                    return false;
                }

                RunType runType = RunType.Docker;

                GlobalAppSettingsRunner globalAppSettingsRunner = new(_configurationService, runType, context);
                InitializeDockerRunner initializeDockerRunner = new(_configurationService, runType, context);
                DevCertsRunner devCertsRunner = new(_configurationService, runType, context);
                BuildDockerImagesRunner buildDockerImagesRunner = new(_configurationService, runType, context);
                DockerDiscoveryServerRunner dockerDiscoveryServerRunner = new(_configurationService, runType, context);

                ApiGatewayRunner apiGatewayRunner = new(_configurationService, config.Framework.ApiGateway, runType, _consulApiService, context);
                HealthChecksDashboardRunner healthChecksDashboardRunner = new(_configurationService, config.Framework.HealthChecksDashboard, runType, _consulApiService, context);

                List<StandardServiceRunner> runners = new();
                foreach (NexusServiceConfiguration? configuration in config.Services)
                {
                    StandardServiceRunner runner = new(_configurationService, configuration, runType, _consulApiService, context);
                    runners.Add(runner);
                }

                for (int i = 0; i < runners.Count - 1; i++)
                {
                    runners[i].AddNextRunner(runners[i + 1]);
                }

                ConsulGlobalConfigRunner consulGlobalConfigRunner =
                    new(_configurationService, _consulApiService, runType, context);
                EnvironmentUpdateRunner environmentUpdateRunner = new(_configurationService, runType, context);
                DockerComposeRunner dockerComposeRunner = new(_configurationService, runType, context);

                runners.Last()
                    .AddNextRunner(consulGlobalConfigRunner)
                    .AddNextRunner(environmentUpdateRunner)
                    .AddNextRunner(dockerComposeRunner);

                globalAppSettingsRunner
                    .AddNextRunner(initializeDockerRunner)
                    .AddNextRunner(devCertsRunner)
                    .AddNextRunner(buildDockerImagesRunner)
                    .AddNextRunner(dockerDiscoveryServerRunner)
                    .AddNextRunner(apiGatewayRunner)
                    .AddNextRunner(healthChecksDashboardRunner)
                    .AddNextRunner(runners[0]);

                _state = globalAppSettingsRunner.Start(_state);

                if (_state.LastStepStatus == StepStatus.Success)
                {
                    return true;
                }

                return false;
            });
        
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Development Environment Setup Successfully[/]");
            PrintState(_state);
            PrintVersion(_state);
            return true;
        }
        
        foreach (string error in _state.Errors)
        {
            AnsiConsole.MarkupLine($"[red]{error}[/]");
        }
        return false;
    }
    
    public bool RunKubernetes()
    {
        bool result = AnsiConsole
            .Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn(),
            })
            .Start(context =>
            {
                NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

                if (config == null)
                {
                    return false;
                }

                RunType runType = RunType.K8s;

                GlobalAppSettingsRunner globalAppSettingsRunner = new(_configurationService, runType, context);
                DevCertsRunner devCertsRunner = new(_configurationService, runType, context);
                BuildDockerImagesRunner buildDockerImagesRunner = new(_configurationService, runType, context);
                PublishDockerImagesRunner publishDockerImagesRunner = new (_configurationService, runType, context);
                DiscoveryServerRunner kubernetesDiscoveryServerRunner = new KubernetesDiscoveryServerRunner(_configurationService, runType, context);
                InfrastructureRunner infrastructureRunner = new KubernetesInfrastructureRunner(_configurationService, context);

                // ApiGatewayRunner apiGatewayRunner = new(_configurationService, config.Framework.ApiGateway, runType, _consulApiService, context);
                // HealthChecksDashboardRunner healthChecksDashboardRunner = new(_configurationService, config.Framework.HealthChecksDashboard, runType, _consulApiService, context);
                //
                // List<StandardServiceRunner> runners = new();
                // foreach (NexusServiceConfiguration? configuration in config.Services)
                // {
                //     StandardServiceRunner runner = new(_configurationService, configuration, runType, _consulApiService, context);
                //     runners.Add(runner);
                // }
                //
                // for (int i = 0; i < runners.Count - 1; i++)
                // {
                //     runners[i].AddNextRunner(runners[i + 1]);
                // }

                ConsulGlobalConfigRunner consulGlobalConfigRunner =
                    new(_configurationService, _consulApiService, runType, context);
                EnvironmentUpdateRunner environmentUpdateRunner = new(_configurationService, runType, context);
                DockerComposeRunner dockerComposeRunner = new(_configurationService, runType, context);

                // runners.Last()
                //     .AddNextRunner(consulGlobalConfigRunner)
                //     .AddNextRunner(environmentUpdateRunner)
                //     .AddNextRunner(dockerComposeRunner);

                globalAppSettingsRunner
                    .AddNextRunner(devCertsRunner)
                    .AddNextRunner(buildDockerImagesRunner)
                    .AddNextRunner(publishDockerImagesRunner)
                    .AddNextRunner(kubernetesDiscoveryServerRunner)
                    .AddNextRunner(infrastructureRunner);
                    // .AddNextRunner(apiGatewayRunner)
                    // .AddNextRunner(healthChecksDashboardRunner)
                    // .AddNextRunner(runners[0]);

                _state = globalAppSettingsRunner.Start(_state);

                if (_state.LastStepStatus == StepStatus.Success)
                {
                    return true;
                }

                return false;
            });
        
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Development Environment Setup Successfully[/]");
            PrintState(_state);
            PrintVersion(_state);
            return true;
        }
        
        foreach (string error in _state.Errors)
        {
            AnsiConsole.MarkupLine($"[red]{error}[/]");
        }
        return false;
    }
}