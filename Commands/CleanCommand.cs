using System.Diagnostics.CodeAnalysis;
using Nexus.Config;
using Nexus.Runners;
using Nexus.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nexus.Commands;

public class CleanSettings : CommandSettings
{
}

public class CleanLocalSettings : CleanSettings
{
}

public class CleanDockerSettings : CleanSettings
{
}

public class CleanLocalCommand : Command<CleanLocalSettings>
{
    public override int Execute([NotNull]CommandContext context, [NotNull]CleanLocalSettings settings)
    {
        AnsiConsole.MarkupLine($"[bold]Cleaning up local development environment[/]");
        ConfigurationService configurationService = new();
        CleanupService cleanupService = new(configurationService);
        bool result = cleanupService.Cleanup(RunType.Local);
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}

public class CleanDockerCommand : Command<CleanDockerSettings>
{
    public override int Execute([NotNull]CommandContext context, [NotNull]CleanDockerSettings settings)
    {
        AnsiConsole.MarkupLine($"[bold]Cleaning up Docker development environment[/]");
        ConfigurationService configurationService = new();
        CleanupService cleanupService = new(configurationService);
        bool result = cleanupService.Cleanup(RunType.Docker);
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}