using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class BuildDockerImagesRunner : ComponentRunner
{
    public BuildDockerImagesRunner(ConfigurationService configurationService, RunType runType, ProgressContext context) 
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Building Docker Images");
        NexusSolutionConfiguration? config = ConfigurationService.ReadConfiguration();
        if (config == null)
        {
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        progressTask.Increment(10);
        
        // Make version
        string version = DateTime.UtcNow.ToString("yyyy.MM.dd.HHmmss");
        
        // Build hc, api-gateway, frontend-app
        progressTask.Description = $"Building Docker Images: frontend-app";
        string command = GetDockerBuildCommand($"{config.SolutionName}-frontend-app", version, config.DockerRepository, ConfigurationService.FrontEndAppDirectory);
        RunDockerCommand(command);
        progressTask.Increment(10);

        progressTask.Description = $"Building Docker Images: api-gateway";
        command = GetDockerBuildCommand($"{config.SolutionName}-api-gateway", version, config.DockerRepository, ConfigurationService.ApiGatewayDockerfile, ConfigurationService.GetBasePath());
        RunDockerCommand(command);
        progressTask.Increment(10);

        progressTask.Description = $"Building Docker Images: health-checks-dashboard";
        command = GetDockerBuildCommand($"{config.SolutionName}-health-checks-dashboard", version, config.DockerRepository, ConfigurationService.HealthChecksDashboardDockerfile, ConfigurationService.GetBasePath());
        RunDockerCommand(command);
        progressTask.Increment(10);

        // Build services
        foreach (NexusServiceConfiguration service in config.Services)
        {
            string serviceName = $"{config.SolutionName}-{service.ServiceName}";
            progressTask.Description = $"Building Docker Images: {serviceName}";
            command = GetDockerBuildCommand(serviceName, version, config.DockerRepository,
                ConfigurationService.GetServiceDockerfile(service.ServiceName, service.ProjectName),
                ConfigurationService.GetBasePath());

            RunDockerCommandV2(command);
            progressTask.Increment((double)1 / config.Services.Count * 50);
        }

        // Save version in state
        state.DockerImageVersion = version;
        state.LastStepStatus = StepStatus.Success;
        progressTask.Description = "Building Docker Images";
        progressTask.Increment(100);
        progressTask.StopTask();
        
        return state;
    }

    protected override string DisplayName => "Docker Images Builder";

    private static string GetDockerBuildCommand(string serviceName, string version, string repo, string folderPath)
    {
        return @$"build -t {serviceName}:latest -t {serviceName}:{version} -t ""{repo}/{serviceName}:latest"" -t ""{repo}/{serviceName}:{version}"" ""{folderPath}"" --progress=plain";
    }

    private static string GetDockerBuildCommand(string serviceName, string version, string repo, string dockerFilePath,
        string contextPath)
    {
        return @$"build -t {serviceName}:latest -t {serviceName}:{version} -t ""{repo}/{serviceName}:latest"" -t ""{repo}/{serviceName}:{version}"" -f ""{dockerFilePath}"" ""{contextPath}"" --progress=plain";
    }
}