namespace Nexus.Core.Config;

public class ApiGatewayConfiguration
{
    public string ServiceName { get; set; } = string.Empty;
    public string ConsulConfigDirectory { get; set; } = string.Empty;
    public string OcelotDirectory { get; set; } = string.Empty;
    public string AppSettingsConfigPath { get; set; } = string.Empty;
}