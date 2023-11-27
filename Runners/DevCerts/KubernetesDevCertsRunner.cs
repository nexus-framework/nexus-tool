using Nexus.Config;
using Spectre.Console;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners.DevCerts;

public class KubernetesDevCertsRunner : DevCertsRunner
{
    public KubernetesDevCertsRunner(ConfigurationService configurationService, ProgressContext context) 
        : base(configurationService, RunType.K8s, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Generating Development Certificates");
        string output = RunPowershellCommand("dotnet dev-certs https -c");

        if (output.Contains("No valid certificate found."))
        {
            RunPowershellCommand("dotnet dev-certs https --trust");
        }

        string certPath = Path.Combine(ConfigurationService.GetBasePath(), "devcerts/aspnetapp.pfx");
        RunPowershellCommand($"dotnet dev-certs https -ep {certPath} -p {state.DevCertsPassword}");
        progressTask.Increment(50);

        string certsFile = ConfigurationService.KubernetesCertificateFile;

        if (!File.Exists(certsFile))
        {
            AddError($"Unable to find file {certsFile}", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

        RunPowershellCommand($"kubectl apply -f \"{certsFile}\"");
        
        progressTask.Increment(50);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}