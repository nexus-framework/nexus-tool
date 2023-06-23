using CommandLine;

namespace Nexus.CliOptions;

[Verb("add", HelpText = "Add file contents to the index.")]
public class AddOptions
{
    [Value(0, Required = true, MetaName = "Type")] public string AddType { get; set; } = string.Empty;

    [Value(1, Required = true, MetaName = "Name")] public string Name { get; set; } = string.Empty;
}