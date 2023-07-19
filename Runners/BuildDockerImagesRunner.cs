using Nexus.Config;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public class BuildDockerImagesRunner : ComponentRunner
{
    public BuildDockerImagesRunner(ConfigurationService configurationService, RunType runType) 
        : base(configurationService, runType)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        NexusSolutionConfiguration? config = ConfigurationService.ReadConfiguration();

        if (config == null)
        {
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        
        // Make version
        string version = DateTime.UtcNow.ToString("yyyy.MM.dd.HHmmss");
        Console.WriteLine($"Building docker images for version {version}");
        
        // Build hc, api-gateway, frontend-app
        string command = GetDockerBuildCommand($"{config.SolutionName}-frontend-app", version, config.DockerRepository, ConfigurationService.FrontEndAppDirectory);
        RunDockerCommand(command, captureOutput: false);

        command = GetDockerBuildCommand($"{config.SolutionName}-api-gateway", version, config.DockerRepository, ConfigurationService.ApiGatewayDockerfile, ConfigurationService.GetBasePath());
        RunDockerCommand(command, captureOutput: false);
        
        command = GetDockerBuildCommand($"{config.SolutionName}-health-checks-dashboard", version, config.DockerRepository, ConfigurationService.HealthChecksDashboardDockerfile, ConfigurationService.GetBasePath());
        RunDockerCommand(command, captureOutput: false);

        // Build services
        foreach (NexusServiceConfiguration service in config.Services)
        {
            string serviceName = $"{config.SolutionName}-{service.ServiceName}";
            command = GetDockerBuildCommand(serviceName, version, config.DockerRepository,
                ConfigurationService.GetServiceDockerfile(service.ServiceName, service.ProjectName),
                ConfigurationService.GetBasePath());

            RunDockerCommand(command, captureOutput: false);
        }
        
        Console.WriteLine($"Built docker images for version {version}");

        // Save version in state
        state.DockerImageVersion = version;
        state.LastStepStatus = StepStatus.Success;
        
        return state;
    }

    protected override string DisplayName => "Docker Images Builder";

    private static string GetDockerBuildCommand(string serviceName, string version, string repo, string folderPath)
    {
        return @$"build -t {serviceName}:latest -t {serviceName}:{version} -t ""{repo}/{serviceName}:latest"" -t ""{repo}/{serviceName}:{version}"" ""{folderPath}""";
    }

    private static string GetDockerBuildCommand(string serviceName, string version, string repo, string dockerFilePath,
        string contextPath)
    {
        return @$"build -t {serviceName}:latest -t {serviceName}:{version} -t ""{repo}/{serviceName}:latest"" -t ""{repo}/{serviceName}:{version}"" -f ""{dockerFilePath}"" ""{contextPath}""";
    }
}