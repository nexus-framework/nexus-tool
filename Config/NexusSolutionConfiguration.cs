namespace Nexus.Config;

public class NexusSolutionConfiguration
{
    public string SolutionName { get; set; } = string.Empty;

    public string DockerRepository { get; set; } = string.Empty;
    public FrameworkConfiguration Framework { get; set; } = new ();
    public List<NexusServiceConfiguration> Services { get; set; } = new ();
}