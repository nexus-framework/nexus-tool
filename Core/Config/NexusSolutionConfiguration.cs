namespace Nexus.Core.Config;

public class NexusSolutionConfiguration
{
    public string ProjectName { get; set; } = string.Empty;

    public FrameworkConfiguration Framework { get; set; } = new ();
    public List<NexusServiceConfiguration> Services { get; set; } = new ();
}