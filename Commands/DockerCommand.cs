using System.Diagnostics.CodeAnalysis;
using Nexus.Config;
using Nexus.Runners;
using Spectre.Console;
using Spectre.Console.Cli;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Commands;

public class DockerSettings : CommandSettings
{
}

public class DockerBuildSettings : DockerSettings
{
}

public class DockerPublishSettings : DockerSettings
{
}


public class DockerBuildCommand : Command<DockerBuildSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] DockerBuildSettings settings)
    {
        AnsiConsole.MarkupLine("[bold]Building Docker Images[/]");
        RunState state = new("", "");
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
            .Start(progressContext =>
            {
                ConfigurationService configurationService = new();
                BuildDockerImagesRunner runner = new(configurationService, RunType.Docker, progressContext);
                runner.Start(state);
                return state.LastStepStatus == StepStatus.Success;
            });

        if (result)
        {
            PrintVersion(state);
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}

public class DockerPublishCommand : Command<DockerBuildSettings>
{
    public override int Execute([NotNull] CommandContext context,[NotNull] DockerBuildSettings settings)
    {
        AnsiConsole.MarkupLine("[bold]Publishing Docker Images[/]");
        RunState state = new("", "");
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
            .Start(progressContext =>
            {
                ConfigurationService configurationService = new();
                
                PublishDockerImagesRunner publishDockerImagesRunner = new(configurationService, RunType.Docker, progressContext);
                BuildDockerImagesRunner buildDockerImagesRunner = new(configurationService, RunType.Docker, progressContext);
                
                buildDockerImagesRunner.AddNextRunner(publishDockerImagesRunner);
                
                buildDockerImagesRunner.Start(state);
                return state.LastStepStatus == StepStatus.Success;
            });

        if (result)
        {
            PrintVersion(state);
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}