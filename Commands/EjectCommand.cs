using Nexus.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nexus.Commands;

public sealed class EjectSettings : CommandSettings
{
}

public sealed class EjectCommand : AsyncCommand<EjectSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, EjectSettings settings)
    {
        if(!AnsiConsole.Confirm("Are you sure you want to eject the libraries? This will replace the library references with source code and is [bold]NOT[/] reversible.", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[red]Aborting[/]");
            return 1;
        }
        
        AnsiConsole.MarkupLine("[bold]Ejecting Libraries[/]");
        SolutionGenerator solutionGenerator = new();
        bool result = await solutionGenerator.Eject();
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }
        
        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}