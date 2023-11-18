using System.ComponentModel;
using Nexus.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nexus.Commands;

public sealed class InitSettings : CommandSettings
{
    [Description("Solution Name")]
    [CommandArgument(0, "<name>")]
    public string Name { get; init; } = string.Empty;
        
    [Description("Include source code for libraries")]
    [CommandOption("--include-library-source")]
    public bool? IncludeLibrarySource { get; init; }
}

public sealed class InitCommand : AsyncCommand<InitSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, InitSettings settings)
    {
        AnsiConsole.MarkupLine("[bold]Initializing Solution[/]");
        SolutionGenerator solutionGenerator = new();
        bool generationResult = await solutionGenerator.InitializeSolution(settings.Name);
        bool ejectResult = true;
        
        if (settings.IncludeLibrarySource.HasValue && settings.IncludeLibrarySource.Value)
        {
            ejectResult = await solutionGenerator.Eject();
        }

        if (generationResult && ejectResult)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}