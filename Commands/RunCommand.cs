using System.Diagnostics.CodeAnalysis;
using Nexus.Config;
using Nexus.Runners;
using Nexus.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nexus.Commands;

public class RunSetings : CommandSettings
{
}

public class RunLocalSettings: RunSetings
{
}

public class RunDockerSettings: RunSetings
{
}

public class RunLocalCommand : Command<RunLocalSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] RunLocalSettings settings)
    {
        AnsiConsole.MarkupLine($"[bold]Running Local Environment[/]");
        
        ConfigurationService configurationService = new();
        ConsulApiService consulApiService = new();
        NexusRunner runner = new(configurationService, consulApiService);
        bool result = runner.RunLocal();
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}

public class RunDockerCommand : Command<RunDockerSettings>
{
    public override int Execute([NotNull]CommandContext context, [NotNull]RunDockerSettings settings)
    {
        AnsiConsole.MarkupLine($"[bold]Running Local Environment[/]");
        
        ConfigurationService configurationService = new();
        ConsulApiService consulApiService = new();
        NexusRunner runner = new(configurationService, consulApiService);
        bool result = runner.RunDocker();
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}