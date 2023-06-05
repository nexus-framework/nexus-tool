using CommandLine;

namespace Nexus.CliOptions;

[Verb("run", HelpText = "Run development environment")]
public class RunOptions
{
    [Value(0, Required = true)] public string Environment { get; set; } = string.Empty;
}