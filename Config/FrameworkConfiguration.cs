namespace Nexus.Config;

public class FrameworkConfiguration
{
    public NexusServiceConfiguration ApiGateway { get; set; } = new ();

    public NexusServiceConfiguration HealthChecksDashboard { get; set; } = new ();
}