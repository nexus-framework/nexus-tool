using System.Net;
using System.Text.RegularExpressions;
using k8s;
using Newtonsoft.Json;
using Nexus.Config;
using Spectre.Console;
using YamlDotNet.RepresentationModel;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Runners;

public abstract class DiscoveryServerRunner : ComponentRunner
{
    protected DiscoveryServerRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }

    protected override string DisplayName => "Discovery Service Runner";
}

public class DockerDiscoveryServerRunner : DiscoveryServerRunner
{
    public DockerDiscoveryServerRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }
    
    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Starting Discovery Server");
        
        string configFolder = ConfigurationService.DiscoveryServerConfigFolder;
        string[] files = Directory.GetFiles(configFolder, "server*.json");
        
        // Replace subnet ip
        ReplaceSubnetIpInConsulServerConfig(files, state.SubnetIp);
        progressTask.Increment(10);

        // Docker compose up
        string dockerComposeYmlPath = ConfigurationService.DiscoveryServerDockerCompose;
        RunPowershellCommand($"docker-compose -f \"{dockerComposeYmlPath}\" up --detach");
        progressTask.Increment(40);

        string consulAclPath = ConfigurationService.DiscoveryServerAcl;

        // Copy ACL to servers
        for (int i = 1; i <= files.Length; i++)
        {
            RunDockerCommand($"cp \"{consulAclPath}\" consul-server{i}:/consul/config/consul-acl.json");
        }
        progressTask.Increment(10);

        // Restart servers
        for (int i = 1; i <= files.Length; i++)
        {
            string containerName = $"consul-server{i}";
            RunDockerCommand($"container restart {containerName}");
            
            string containerIp = string.Empty;
            int retries = 0;
            
            while (containerIp == string.Empty && retries <= 5)
            {
                containerIp =
                    RunDockerCommand(
                        $"container inspect {containerName} --format {{{{.NetworkSettings.Networks.{state.NetworkName}.IPAddress}}}}");

                if (IPAddress.TryParse(containerIp, out _))
                {
                    break;
                }
                
                retries++;
                Thread.Sleep(5000);
            }
        }
        progressTask.Increment(10);
        
        // Check if anonymous ACL has been created
        string[] containerNames = Enumerable
            .Range(1, files.Length)
            .Select(x => $"consul-server{x}")
            .ToArray();
        for (int logRetry = 0; logRetry < 5; logRetry++)
        {
            string logs = string.Join('\n', containerNames.Select(x => RunDockerCommand($"logs {x}")));
            if (logs.Contains("Created ACL anonymous token from configuration"))
            {
                break;
            }
            Thread.Sleep(2500);
        }
        progressTask.Increment(10);

        // Bootstrap acl
        string bootstrapOutput = RunDockerCommand("exec consul-server1 consul acl bootstrap");
        Match match = Regex.Match(bootstrapOutput, @"SecretID:\s+(\S+)");
        state.GlobalToken = match.Success ? match.Groups[1].Value : string.Empty;
        
        if (string.IsNullOrEmpty(state.GlobalToken))
        {
            AddError("Unable to Bootstrap Consul ACL", state);
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        progressTask.Increment(10);
        
        // Set agent tokens
        for (int i = 1; i <= files.Length; i++)
        {
            RunDockerCommand(
                $"exec -e CONSUL_HTTP_TOKEN=\"{state.GlobalToken}\" consul-server{i} consul acl set-agent-token agent \"{state.GlobalToken}\"");
        }
        
        progressTask.Increment(10);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    

    private void ReplaceSubnetIpInConsulServerConfig(string[] files, string subnetIp)
    {
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);

            if (string.IsNullOrEmpty(json))
            {
                continue;
            }

            dynamic? config = JsonConvert.DeserializeObject<dynamic>(json);

            if (config == null)
            {
                continue;
            }

            config.bind_addr = $"{{{{ GetPrivateInterfaces | include \"network\" \"{subnetIp}\" | attr \"address\" }}}}";

            string updatedJson =
                JsonConvert.SerializeObject(config, Formatting.Indented);

            File.WriteAllText(file, updatedJson);
        }
    }
}

public class KubernetesDiscoveryServerRunner : DockerDiscoveryServerRunner
{
    public KubernetesDiscoveryServerRunner(ConfigurationService configurationService, RunType runType, ProgressContext context)
        : base(configurationService, runType, context)
    {
    }

    protected override RunState OnExecuted(RunState state)
    {
        ProgressTask progressTask = Context.AddTask("Starting Discovery Server");

        string consulYaml = ConfigurationService.KuberetesConsulFile;

        if (!File.Exists(consulYaml))
        {
            AddError($"File not found: {consulYaml}", state);
            state.LastStepStatus = StepStatus.Failure;
            progressTask.StopTask();
            return state;
        }
        progressTask.Increment(10);

        RunPowershellCommand($"kubectl apply -f \"{consulYaml}\"");
        state.GlobalToken = "dev123";
        progressTask.Increment(10);
        
        // Wait for consul to be up
        RunPowershellCommand($"kubectl wait --for=condition=ready pod -l app=consul -n nexus --timeout=300s");
        progressTask.Increment(10);
        Thread.Sleep(10 * 1000);
        
        progressTask.Increment(100);
        progressTask.StopTask();
        state.LastStepStatus = StepStatus.Success;
        return state;
    }
}