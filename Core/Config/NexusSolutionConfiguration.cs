namespace Nexus.Core.Config;

public class NexusSolutionConfiguration
{
    public string ProjectName { get; set; } = string.Empty;

    public List<NexusServiceConfiguration> Services { get; set; } = new List<NexusServiceConfiguration>();
}