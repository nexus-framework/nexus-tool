using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using Colorful;
using Newtonsoft.Json;
using Nexus.Config;
using static Nexus.Extensions.ConsoleUtilities;
using Console = Colorful.Console;

namespace Nexus.Runners;

public class DiscoveryServerRunner : ComponentRunner
{
    public DiscoveryServerRunner(ConfigurationService configurationService, RunType runType) : base(configurationService, runType)
    {
    }
    
    protected override RunState OnExecuted(RunState state)
    {
        string configFolder = ConfigurationService.DiscoveryServerConfigFolder;
        string[] files = Directory.GetFiles(configFolder, "server*.json");
        
        // Replace subnet ip
        Console.WriteLine("Updating consul server configs");
        ReplaceSubnetIpInConsulServerConfig(files, state.SubnetIp);

        // Docker compose up
        Console.WriteLine("Starting consul server on docker");
        string dockerComposeYmlPath = ConfigurationService.DiscoveryServerDockerCompose;
        RunPowershellCommand($"docker-compose -f \"{dockerComposeYmlPath}\" up --detach");

        string consulAclPath = ConfigurationService.DiscoveryServerAcl;

        // Copy ACL to servers
        Console.WriteLine("Copying configs to docker containers");
        for (int i = 1; i <= files.Length; i++)
        {
            RunDockerCommand($"cp \"{consulAclPath}\" consul-server{i}:/consul/config/consul-acl.json");
        }

        // Restart servers
        for (int i = 1; i <= files.Length; i++)
        {
            string containerName = $"consul-server{i}";
            
            StyleSheet ss = new StyleSheet(Color.Black);
            ss.AddStyle(containerName, Color.Cyan);
            Console.WriteLineStyled($"Restarting consul server: {containerName}", ss);

            RunDockerCommand($"container restart {containerName}");
            
            string containerIp = string.Empty;
            int retries = 0;
            
            while (containerIp == string.Empty && retries <= 5)
            {
                Console.WriteLine($"Waiting for {containerName} to be up again (try: {retries+1})...");
                containerIp =
                    RunDockerCommand(
                        $"container inspect {containerName} --format {{{{.NetworkSettings.Networks.{state.NetworkName}.IPAddress}}}}");

                if (IPAddress.TryParse(containerIp, out _))
                {
                    Console.WriteLine($"{containerName} is up");
                    Thread.Sleep(1500);
                    break;
                }
                
                retries++;
                Thread.Sleep(5000);
            }
        }
        
        // Bootstrap acl
        string bootstrapOutput = RunDockerCommand("exec consul-server1 consul acl bootstrap");
        Match match = Regex.Match(bootstrapOutput, @"SecretID:\s+(\S+)");
        state.GlobalToken = match.Success ? match.Groups[1].Value : string.Empty;
        
        if (string.IsNullOrEmpty(state.GlobalToken))
        {
            Console.Error.WriteLine("Unable to Bootstrap Consul ACL");
            state.LastStepStatus = StepStatus.Failure;
            return state;
        }
        Console.WriteLine($"Discovery Server ACL Bootstrap Done. Token: {state.GlobalToken}");
        
        // Set agent tokens
        for (int i = 1; i <= files.Length; i++)
        {
            Console.WriteLine($"Setting token for consul-server{i}");
            RunDockerCommand(
                $"exec -e CONSUL_HTTP_TOKEN=\"{state.GlobalToken}\" consul-server{i} consul acl set-agent-token agent \"{state.GlobalToken}\"");
        }
        
        state.LastStepStatus = StepStatus.Success;
        return state;
    }

    protected override string DisplayName => "Discovery Service Runner";

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
