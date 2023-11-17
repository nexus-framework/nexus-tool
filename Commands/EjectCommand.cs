using Nexus.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nexus.Commands;

internal sealed class EjectCommand : AsyncCommand<EjectCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[bold]Ejecting Libraries[/]");
        SolutionGenerator solutionGenerator = new();
        var result = await solutionGenerator.Eject();
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }
        
        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}