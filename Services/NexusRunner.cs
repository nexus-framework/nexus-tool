using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using JsonConverter = Newtonsoft.Json.JsonConverter;

namespace Nexus.Services;

internal class NexusRunner
{
    private readonly ConfigurationService _configurationService;
    private const string NetworkName = "consul_external";
    private string networkId = string.Empty;
    private string subnetIp = string.Empty;
    private string globalToken = string.Empty;

    public NexusRunner(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    public int RunLocal()
    {
        SetupDevCerts("dev123");
        PreconfigureDocker();
        SetupDiscoveryServer();
        return 0;
    }

    public int RunDocker()
    {
        throw new NotImplementedException();
    }

    private void SetupDevCerts(string password)
    {
        string output = RunPowershellCommand("dotnet dev-certs https -c");

        if (output.Contains("No valid certificate found."))
        {
            RunPowershellCommand("dotnet dev-certs https --trust");
        }

        string certPath = Path.Combine(_configurationService.GetBasePath(), "devcerts/aspnetapp.pfx");
        RunPowershellCommand($"dotnet dev-certs https -ep {certPath} -p {password}");
    }
    
    private void PreconfigureDocker()
    {
        Console.WriteLine("Increasing WSL memory for ELK");
        RunPowershellCommand(@"wsl -d docker-desktop sysctl -w ""vm.max_map_count=262144""");
        
        Console.WriteLine("Checking Docker Networks");

        string networkList = RunDockerCommand($"network ls --filter \"name={NetworkName}\" --format \"{{{{.Name}}}}\"");
        
        if (networkList.Contains(NetworkName))
        {
            Console.WriteLine($"The network {NetworkName} already exists");
        }
        else
        {
            RunDockerCommand($"network create {NetworkName}");
            Console.WriteLine($"The network {NetworkName} has been created");
        }

        networkId = RunDockerCommand($"network inspect {NetworkName} \"--format={{{{.Id}}}}\"");
        subnetIp = RunDockerCommand($"network inspect {networkId} \"--format={{{{(index .IPAM.Config 0).Subnet}}}}");
        
        Console.WriteLine($"Subnet IP: {subnetIp}");
    }

    private void SetupDiscoveryServer()
    {
        string configFolder = Path.Combine(_configurationService.GetBasePath(), @"discovery-server\docker\");
        string[] files = Directory.GetFiles(configFolder, "server*.json");
        
        // Replace subnet ip
        ReplaceSubnetIpInConsulServerConfig(files);

        // Docker compose up
        string dockerComposeYmlPath = Path.Combine(configFolder, "docker-compose.yml");
        RunPowershellCommand($"docker-compose -f \"{dockerComposeYmlPath}\" up --detach");

        string consulAclPath = Path.Combine(configFolder, "consul-acl.json");

        // Copy ACL to servers
        for (int i = 1; i <= files.Length; i++)
        {
            RunDockerCommand($"cp \"{consulAclPath}\" consul-server{i}:/consul/config/consul-acl.json");
        }

        // Restart servers
        for (int i = 1; i <= files.Length; i++)
        {
            string containerName = $"consul-server{i}";
            RunDockerCommand($"container restart {containerName}");
            string containerIp = string.Empty;
            int retries = 0;
            while (containerIp == string.Empty && retries <= 5)
            {
                Console.WriteLine($"Waiting for {containerName} to be up again (try: {retries+1})...");
                containerIp =
                    RunDockerCommand(
                        $"container inspect {containerName} --format {{{{.NetworkSettings.Networks.{NetworkName}.IPAddress}}}}");

                if (IPAddress.TryParse(containerIp, out _))
                {
                    Console.WriteLine($"{containerName} is up");
                    break;
                }
                
                retries++;
                Thread.Sleep(5000);
            }
        }
        
        // Bootstrap acl
        var bootstrapOutput = RunDockerCommand("exec consul-server1 consul acl bootstrap");
        Match match = Regex.Match(bootstrapOutput, @"SecretID:\s+(\S+)");
        globalToken = match.Success ? match.Groups[1].Value : string.Empty;
        
        // Set agent tokens
        for (int i = 1; i <= files.Length; i++)
        {
            RunDockerCommand(
                $"exec -e CONSUL_HTTP_TOKEN=\"{globalToken}\" consul-server{i} consul acl set-agent-token agent \"{globalToken}\"");
        }
        
        Console.WriteLine("Discovery Server ACL Bootstrap Done");
    }

    private void ReplaceSubnetIpInConsulServerConfig(string[] files)
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

            dynamic? updatedJson =
                JsonConvert.SerializeObject(config, new JsonSerializerSettings { Formatting = Formatting.Indented });

            File.WriteAllText(file, updatedJson);
        }
    }

    private string RunPowershellCommand(string command)
    {
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;

            process.Start();

            output = process.StandardOutput.ReadToEnd();
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }

        return output;
    }
    
    private string RunDockerCommand(string command, bool redirectStandard = true)
    {
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = command;

            if (redirectStandard)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }
            
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;

            process.Start();

            if (redirectStandard)
            {
                output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd();
            }
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }

        return output;
    }
    
    private string RunDockerComposeCommand(string command)
    {
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "docker-compose";
            process.StartInfo.Arguments = command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;

            process.Start();

            output = process.StandardOutput.ReadToEnd().Trim();
            var error = process.StandardError.ReadToEnd();
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }

        return output;
    }
}