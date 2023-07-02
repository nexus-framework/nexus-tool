using Nexus.Config;
using Nexus.Runners;
using static Nexus.Extensions.ConsoleUtilities;

namespace Nexus.Services;

public class CleanupService
{
    private readonly ConfigurationService _configurationService;

    public CleanupService(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }
    
    public void Cleanup(RunType runType)
    {
        string dockerComposePath = _configurationService.GetDockerComposePath(runType);
        string discoveryServerDockerComposePath = _configurationService.DiscoveryServerDockerCompose;
        
        string command = $"docker-compose -f \"{dockerComposePath}\" down -v";
        RunPowershellCommand(command, false);
        
        command = $"docker-compose -f \"{discoveryServerDockerComposePath}\" down -v";
        RunPowershellCommand(command, false);
    }
}
