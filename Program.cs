using CommandLine;
using Nexus.CliOptions;
using Nexus.Config;
using Nexus.Generators;
using Nexus.Runners;
using Nexus.Services;

namespace Nexus;

public class Program
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<AddServiceOptions, InitOptions, RunOptions, EjectOptions>(args)
            .MapResult(
                (AddServiceOptions opts) => AddServiceAndReturnExitCode(opts),
                (InitOptions opts) => InitAndReturnExitCode(opts),
                (RunOptions opts) => RunAndReturnExitCode(opts),
                (EjectOptions opts) => Eject(opts),
                errs => 1);
    }

    private static int Eject(EjectOptions opts)
    {
        SolutionGenerator solutionGenerator = new ();
        return solutionGenerator.Eject().Result ? 0 : 1;
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

    static int AddServiceAndReturnExitCode(AddServiceOptions addServiceOptions)
    {
        SolutionGenerator solutionGenerator = new ();
        return solutionGenerator.AddService(addServiceOptions.Name).Result ? 0 : 1;
    }
    
    static int InitAndReturnExitCode(InitOptions options)
    {
        SolutionGenerator solutionGenerator = new ();
        bool initResult = solutionGenerator.InitializeSolution(options.Name).Result;

        if (!initResult)
        {
            return 1;
        }
        if (options.IncludeLibrarySource)
        {
            return solutionGenerator.Eject().Result ? 0 : 1;
        }
        
        Console.WriteLine("Done");
        return 0;
    }
}