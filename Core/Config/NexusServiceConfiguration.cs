namespace Nexus.Core.Config;

public class NexusServiceConfiguration
{
    public string ServiceName { get; set; } = String.Empty;
    public string ConsulConfigDirectory { get; set; } = String.Empty;
    public string AppSettingsConfigPath { get; set; } = String.Empty;
    public int Port { get; set; }
}