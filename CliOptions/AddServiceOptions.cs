using CommandLine;
using CommandLine.Text;

namespace Nexus.CliOptions;

[Verb("add-service", HelpText = "Add a new service")]
public class AddServiceOptions
{
     [Option('n', "name", Required = true, HelpText = "Name of the new service")]
     public string Name { get; set; } = string.Empty;
}