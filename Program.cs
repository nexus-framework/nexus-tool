using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Cocona;
using Cocona.Builder;
using Nexus.Commands;
using Nexus.Services;
using Pastel;
using Spectre.Console;
using Spectre.Console.Cli;
using Color = Spectre.Console.Color;

namespace Nexus;

public class Program
{
    public static void Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("Nexus Framework").LeftJustified().Color(Color.Green));
        
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<InitCommand>("init")
                .WithDescription("Create a new Nexus Solution");

            config.AddCommand<EjectCommand>("eject")
                .WithDescription("Replace library references with source code");
            
            // config.AddCommand<AddCommand>("add")
            //     .WithDescription("Add components to the solution");
            //
            // config.AddCommand<RunCommand>("run")
            //     .WithDescription("Run development environment");
            //
            // config.AddCommand<CleanCommand>("clean")
            //     .WithDescription("Clean up the dev environment");
            //
            // config.AddCommand<DockerCommand>("docker")
            //     .WithDescription("Docker specific commands");
        });
        
        app.Run(args);
    }
    // public static void Main(string[] args)
    // {
    //     Console.WriteLine(Constants.NexusLogo.Pastel(Constants.Colors.Info));
    //
    //     CoconaAppBuilder builder = CoconaApp.CreateBuilder();
    //     CoconaApp app = builder.Build();
    //
    //     app.AddCommand("init", Commands.InitSolution)
    //         .WithDescription("Create a new Nexus Solution");
    //
    //     app.AddCommand("eject", Commands.Eject)
    //         .WithDescription("Replace library references with source code");
    //
    //     app.AddSubCommand("add", x =>
    //         {
    //             x.AddCommand("service", Commands.AddService)
    //                 .WithDescription("Add a new service");
    //         })
    //         .WithDescription("Add components to the solution");
    //
    //     app.AddSubCommand("run", x =>
    //         {
    //             x.AddCommand("local", Commands.RunLocal).WithDescription("Run local development environment");
    //             x.AddCommand("docker", Commands.RunDocker).WithDescription("Run docker development environment");
    //         })
    //         .WithDescription("Run development environment");
    //
    //     app.AddSubCommand("clean", options =>
    //         {
    //             options.AddCommand("local", Commands.CleanLocal)
    //                 .WithDescription("Clean up the local dev environment");
    //
    //             options.AddCommand("docker", Commands.CleanDocker)
    //                 .WithDescription("Clean up the docker dev environment");
    //         })
    //         .WithDescription("Clean up the dev environment");
    //
    //     app.AddSubCommand("docker", options =>
    //         {
    //             options.AddCommand("build", Commands.DockerBuild)
    //                 .WithDescription("Build docker images for services");
    //
    //             options.AddCommand("publish", Commands.DockerPublish)
    //                 .WithDescription("Publish docker images for services");
    //         })
    //         .WithDescription("Docker specific commands");
    //
    //     app.Run();       
    // }
}

