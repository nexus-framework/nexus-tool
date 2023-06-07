using CommandLine;
using Nexus.CliOptions;
using Nexus.Services;

namespace Nexus;

public class Program
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<AddOptions, InitOptions, RunOptions>(args)
            .MapResult(
                (AddOptions opts) => AddAndReturnExitCode(opts),
                (InitOptions opts) => InitAndReturnExitCode(opts),
                (RunOptions opts) => RunAndReturnExitCode(opts),
                errs => 1);
    }

    private static int RunAndReturnExitCode(RunOptions runOptions)
    {
        ConfigurationService configurationService = new ();
        ConsulApiService consulApiService = new ();
        NexusRunner runner = new (configurationService, consulApiService);
        return runOptions.Environment.Trim().ToLower() switch
        {
            "local" => runner.RunLocal(),
            "docker" => runner.RunDocker(),
            _ => 1,
        };
    }

    static int AddAndReturnExitCode(AddOptions addOptions)
    {
        SolutionGenerator solutionGenerator = new ();
        return addOptions.AddType.Trim().ToLower() switch
        {
            "service" => solutionGenerator.AddService(addOptions.Name) ? 0 : 1,
            _ => 1,
        };
    }
    
    static int InitAndReturnExitCode(InitOptions options)
    {
        SolutionGenerator solutionGenerator = new ();
        return solutionGenerator.InitializeSolution(options.Name) ? 0 : 1;
    }
}