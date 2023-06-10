namespace Nexus.Config;

public class NexusServiceConfiguration
{
    public string ServiceName { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;
    
    public string RootNamespace { get; set; } = string.Empty;
    
    public int Port { get; set; }
    
    public int? DbPort { get; set; }
}