using Nexus.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nexus.Commands;

public class AddSettings : CommandSettings
{
}
    
public class AddServiceSettings : AddSettings
{
    [CommandArgument(0, "<name>")]
    public string Name { get; init; }
}

public class AddServiceCommand : AsyncCommand<AddServiceSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddServiceSettings settings)
    {
        AnsiConsole.MarkupLine($"[bold]Adding Service [green]{settings.Name}[/][/]");
        SolutionGenerator solutionGenerator = new();
        bool result = await solutionGenerator.AddService(settings.Name);
        if (result)
        {
            AnsiConsole.MarkupLine("[green]Done[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Completed with errors[/]");
        return 1;
    }
}

