using Nexus.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using Color = Spectre.Console.Color;

namespace Nexus;

public class Program
{
    public static void Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Nexus Framework").LeftJustified().Color(Color.Green));
        
        CommandApp app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<InitCommand>("init")
                .WithDescription("Create a new Nexus Solution");

            config.AddCommand<EjectCommand>("eject")
                .WithDescription("Replace library references with source code");

            config.AddBranch<AddSettings>("add", options =>
            {
                options.AddCommand<AddServiceCommand>("service")
                    .WithDescription("Add a new service");
            });
            
            config.AddBranch<RunSetings>("run", options =>
            {
                options.AddCommand<RunLocalCommand>("local")
                    .WithDescription("Run local development environment");
                options.AddCommand<RunDockerCommand>("docker")
                    .WithDescription("Run Docker development environment");
            });
            
            config.AddBranch<CleanSettings>("clean", options =>
            {
                options.AddCommand<CleanLocalCommand>("local")
                    .WithDescription("Clean up local development environment");
                options.AddCommand<CleanDockerCommand>("docker")
                    .WithDescription("Clean up Docker development environment");
            });
            
            config.AddBranch<DockerSettings>("docker", options =>
            {
                options.AddCommand<DockerBuildCommand>("build")
                    .WithDescription("Build docker images for services");
                options.AddCommand<DockerPublishCommand>("publish")
                    .WithDescription("Publish docker images for services");
            });

        });
        
        app.Run(args);
    }
}

