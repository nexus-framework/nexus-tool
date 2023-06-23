using CommandLine;

namespace Nexus.CliOptions;

[Verb("init", HelpText = "Add file contents to the index.")]
public class InitOptions
{
    [Value(0, Required = true, MetaName = "name")] public string Name { get; set; } = string.Empty;
    
    [Option('s', "include-lib-src", Required = false, HelpText = "Include Library Source")]
    public bool IncludeLibrarySource { get; set; } = false;
}