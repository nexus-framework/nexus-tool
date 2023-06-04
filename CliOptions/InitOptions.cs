using CommandLine;

namespace Nexus.CliOptions;

[Verb("init", HelpText = "Add file contents to the index.")]
public class InitOptions
{
    [Value(0, Required = true)] public string Name { get; set; } = string.Empty;
}