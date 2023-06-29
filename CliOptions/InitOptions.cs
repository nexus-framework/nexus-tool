using CommandLine;

namespace Nexus.CliOptions;

[Verb("init", HelpText = "Add file contents to the index.")]
public class InitOptions
{
    [Option('n', "name", Required = true, HelpText = "Name of the solution")]
    public string Name { get; set; } = string.Empty;
    
    [Option('d', "docker-repository", Required = false, Default = "", HelpText = "Name of the docker repository")]
    public string DockerRepository { get; set; } = string.Empty;
    
    [Option('s', "include-lib-src", Required = false, HelpText = "Include source code for nexus-libraries")]
    public bool IncludeLibrarySource { get; set; } = false;
}