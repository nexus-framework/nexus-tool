using Cocona;
using Cocona.Builder;
using Nexus.Config;
using Nexus.Runners;
using Nexus.Services;

CoconaAppBuilder builder = CoconaApp.CreateBuilder();
CoconaApp app = builder.Build();

static async Task InitSolution(
    [Option('n', Description = "Solution name")] string name,
    [Option('l', Description = "Include source code for libraries")] bool includeLibrarySource)
{
    SolutionGenerator solutionGenerator = new ();
    await solutionGenerator.InitializeSolution(name);
    
    if (includeLibrarySource)
    {
        await solutionGenerator.Eject();
    }
    
    Console.WriteLine("Done");
}

static async Task Eject()
{
    SolutionGenerator solutionGenerator = new ();
    await solutionGenerator.Eject();
}

static async Task AddService(
    [Option(shortName: 'n', Description = "Service name", ValueName = "name")] string name)
{
    SolutionGenerator solutionGenerator = new ();
    await solutionGenerator.AddService(name);
}

static void RunLocal()
{
    ConfigurationService configurationService = new ();
    ConsulApiService consulApiService = new ();
    NexusRunner runner = new (configurationService, consulApiService);
    runner.RunLocal();
}

static void RunDocker()
{
    ConfigurationService configurationService = new ();
    ConsulApiService consulApiService = new ();
    NexusRunner runner = new (configurationService, consulApiService);
    runner.RunDocker();
}

static void CleanLocal()
{
    ConfigurationService configurationService = new ();
    CleanupService cleanupService = new (configurationService);
    cleanupService.Cleanup(RunType.Local);
}

static void CleanDocker()
{
    ConfigurationService configurationService = new ();
    CleanupService cleanupService = new (configurationService);
    cleanupService.Cleanup(RunType.Docker);
}

app.AddCommand("init", InitSolution)
    .WithDescription("Create a new Nexus Solution");

app.AddCommand("eject", Eject)
    .WithDescription("Replace library references with source code");

app.AddSubCommand("add", x =>
    {
        x.AddCommand("service", AddService)
            .WithDescription("Add a new service");
    })
    .WithDescription("Add components to the solution");

app.AddSubCommand("run", x =>
    {
        x.AddCommand("local", RunLocal).WithDescription("Run local development environment");
        x.AddCommand("docker", RunDocker).WithDescription("Run docker development environment");
    })
    .WithDescription("Run development environment");

app.AddSubCommand("clean", options =>
    {
        options.AddCommand("local", CleanLocal)
            .WithDescription("Clean up the local dev environment");
        
        options.AddCommand("docker", CleanDocker)
            .WithDescription("Clean up the docker dev environment");
    })
    .WithDescription("Clean up the dev environment");

app.Run();