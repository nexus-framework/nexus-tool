using CommandLine;
using Nexus.CliOptions;
using Nexus.Services;

namespace Nexus;

public class Program
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<AddOptions, InitOptions>(args)
            .MapResult(
                (AddOptions opts) => RunAddAndReturnExitCode(opts),
                (InitOptions opts) => RunInitAndReturnExitCode(opts),
                errs => 1);
    }

    static int RunAddAndReturnExitCode(AddOptions addOptions)
    {
        SolutionGenerator solutionGenerator = new SolutionGenerator();
        return addOptions.AddType.Trim().ToLower() switch
        {
            "service" => solutionGenerator.AddService(addOptions.Name) ? 0 : 1,
            _ => 1,
        };
    }
    
    static int RunInitAndReturnExitCode(InitOptions options)
    {
        SolutionGenerator solutionGenerator = new SolutionGenerator();
        return solutionGenerator.InitializeSolution(options.Name) ? 0 : 1;
    }
}