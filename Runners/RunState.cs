using System.Text;
using Nexus.Models;
using Spectre.Console;

namespace Nexus.Runners;

public class RunState
{
    public RunState(string networkName, string devCertsPassword)
    {
        NetworkName = networkName;
        DevCertsPassword = devCertsPassword;
    }
    
    public string NetworkName { get; set; }
    public string NetworkId { get; set; } = string.Empty;
    public string SubnetIp { get; set; } = string.Empty;
    public string GlobalToken { get; set; } = string.Empty;
    public string DevCertsPassword { get; set; }
    
    public Dictionary<string, PolicyCreationResult> Policies = new ();
    public StepStatus LastStepStatus { get; set; }
    public Dictionary<string, string> ServiceUrls { get; set; } = new ();
    public string DockerImageVersion { get; set; } = string.Empty;

    public Dictionary<string, string> ServiceTokens = new ();
    public List<string> Errors { get; set; } = new ();
    public List<string> Logs { get; set; } = new ();

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("DEBUG: State Start");

        sb.AppendLine($"NetworkName: {NetworkName}");
        sb.AppendLine($"NetworkId: {NetworkId}");
        sb.AppendLine($"SubnetIp: {SubnetIp}");
        sb.AppendLine($"GlobalToken: {GlobalToken}");
        sb.AppendLine($"DevCertsPassword: {DevCertsPassword}");

        foreach(var policy in Policies)
        {
            sb.AppendLine($"Policy Key: {policy.Key}, Policy Creation Result: {policy.Value}");
        }

        sb.AppendLine($"LastStepStatus: {LastStepStatus}");

        foreach(var url in ServiceUrls)
        {
            sb.AppendLine($"Service Key: {url.Key}, Url: {url.Value}");
        }

        sb.AppendLine($"DockerImageVersion: {DockerImageVersion}");

        foreach(var token in ServiceTokens)
        {
            sb.AppendLine($"Service Token Key: {token.Key}, Token: {token.Value}");
        }

        foreach(var error in Errors)
        {
            sb.AppendLine($"Error: {error}");
        }

        sb.AppendLine("DEBUG: State End");

        return sb.ToString();
    }
}