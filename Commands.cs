using System.Drawing;
using Cocona;
using Nexus.Config;
using Nexus.Runners;
using Nexus.Services;
using Console = Colorful.Console;
namespace Nexus;

internal static class Commands
{
    internal static async Task InitSolution(
        [Option('n', Description = "Solution name")]
        string name,
        [Option('l', Description = "Include source code for libraries")]
        bool includeLibrarySource,
        CancellationToken cancellationToken = default)
    {
        SolutionGenerator solutionGenerator = new();
        await solutionGenerator.InitializeSolution(name, cancellationToken);

        if (includeLibrarySource)
        {
            await solutionGenerator.Eject();
        }

        Console.WriteLine("Done",  Color.Green);
    }

    internal static async Task Eject()
    {
        SolutionGenerator solutionGenerator = new();
        await solutionGenerator.Eject();
        Console.WriteLine("Done",  Color.Green);
    }

    internal static async Task AddService(
        [Option(shortName: 'n', Description = "Service name", ValueName = "name")]
        string name)
    {
        SolutionGenerator solutionGenerator = new();
        await solutionGenerator.AddService(name);
        Console.WriteLine("Done",  Color.Green);
    }

    internal static void RunLocal()
    {
        ConfigurationService configurationService = new();
        ConsulApiService consulApiService = new();
        NexusRunner runner = new(configurationService, consulApiService);
        runner.RunLocal();
        Console.WriteLine("Done",  Color.Green);
    }

    internal static void RunDocker()
    {
        ConfigurationService configurationService = new();
        ConsulApiService consulApiService = new();
        NexusRunner runner = new(configurationService, consulApiService);
        runner.RunDocker();
        Console.WriteLine("Done",  Color.Green);
    }

    internal static void CleanLocal()
    {
        ConfigurationService configurationService = new();
        CleanupService cleanupService = new(configurationService);
        cleanupService.Cleanup(RunType.Local);
        Console.WriteLine("Done",  Color.Green);
    }

    internal static void CleanDocker()
    {
        ConfigurationService configurationService = new();
        CleanupService cleanupService = new(configurationService);
        cleanupService.Cleanup(RunType.Docker);
        Console.WriteLine("Done",  Color.Green);
    }

    internal static void DockerBuild()
    {
        ConfigurationService configurationService = new();
        BuildDockerImagesRunner runner = new(configurationService, RunType.Docker);
        RunState state = new("", "");
        runner.Start(state);
        Console.WriteLine("Done",  Color.Green);
    }

    internal static void DockerPublish()
    {
        ConfigurationService configurationService = new();
        PublishDockerImagesRunner publishDockerImagesRunner = new(configurationService, RunType.Docker);
        BuildDockerImagesRunner buildDockerImagesRunner = new(configurationService, RunType.Docker);

        buildDockerImagesRunner.AddNextRunner(publishDockerImagesRunner);

        RunState state = new("", "");
        buildDockerImagesRunner.Start(state);
        Console.WriteLine("Done",  Color.Green);
    }
}