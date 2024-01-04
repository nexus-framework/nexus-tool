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
        AddLog("Generating Development Certificates", state);
        string output = RunPowershellCommand("dotnet dev-certs https -c");

        if (output.Contains("No valid certificate found."))
        {
            AddLog("No valid certificate found.", state);
            RunPowershellCommand("dotnet dev-certs https --trust");
            AddLog("Created certificate", state);
        }

        string certPath = Path.Combine(ConfigurationService.GetBasePath(), "devcerts/aspnetapp.pfx");
        RunPowershellCommand($"dotnet dev-certs https -ep {certPath} -p {state.DevCertsPassword}");
        AddLog("Added certificate password", state);
        progressTask.Increment(50);

        string certsFile = ConfigurationService.KubernetesCertificateFile;

        if (!File.Exists(certsFile))
        {
            AddError($"Unable to find file {certsFile}", state);
            AddLog($"Unable to find file {certsFile}", state);
            progressTask.StopTask();
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }

        AddLog("Adding cert as a k8s secret", state);
        RunPowershellCommand($"kubectl apply -f \"{certsFile}\"");
        AddLog("Added cert as a k8s secret", state);
        
        progressTask.Increment(50);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}