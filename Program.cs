using System.Drawing;
using Cocona;
using Cocona.Builder;
using Console = Colorful.Console;

namespace Nexus;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteAscii("NEXUS", Color.FromArgb(246, 148, 137));

        CoconaAppBuilder builder = CoconaApp.CreateBuilder();
        CoconaApp app = builder.Build();

        app.AddCommand("init", Commands.InitSolution)
            .WithDescription("Create a new Nexus Solution");

        app.AddCommand("eject", Commands.Eject)
            .WithDescription("Replace library references with source code");

        app.AddSubCommand("add", x =>
            {
                x.AddCommand("service", Commands.AddService)
                    .WithDescription("Add a new service");
            })
            .WithDescription("Add components to the solution");

        app.AddSubCommand("run", x =>
            {
                x.AddCommand("local", Commands.RunLocal).WithDescription("Run local development environment");
                x.AddCommand("docker", Commands.RunDocker).WithDescription("Run docker development environment");
            })
            .WithDescription("Run development environment");

        app.AddSubCommand("clean", options =>
            {
                options.AddCommand("local", Commands.CleanLocal)
                    .WithDescription("Clean up the local dev environment");

                options.AddCommand("docker", Commands.CleanDocker)
                    .WithDescription("Clean up the docker dev environment");
            })
            .WithDescription("Clean up the dev environment");

        app.AddSubCommand("docker", options =>
            {
                options.AddCommand("build", Commands.DockerBuild)
                    .WithDescription("Build docker images for services");

                options.AddCommand("publish", Commands.DockerPublish)
                    .WithDescription("Publish docker images for services");
            })
            .WithDescription("Docker specific commands");

        app.Run();       
    }
}