namespace Nexus.Core.Config;

public class FrameworkConfiguration
{
    public ApiGatewayConfiguration ApiGateway { get; set; } = new ();

    public HealthChecksDashboardConfiguration HealthChecksDashboard { get; set; } = new ();
}