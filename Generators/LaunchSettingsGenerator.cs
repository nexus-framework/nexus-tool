namespace Nexus.Generators.LaunchSettings;

public class LaunchSettingsGenerator : ProjectCodeGenerator
{
    private readonly Random _random;
    private const string LaunchSettingsJsonTemplatePath = "Templates/LaunchSettings/launchSettings.json.txt";
    private const string LaunchSettingsJsonOutputPath = "Properties/launchSettings.json";
    public LaunchSettingsGenerator()
    {
        _random = new Random();
    }
    
    public override bool GenerateFiles(string csProjFolderPath, string projectName, string serviceName)
    {
        return GenerateLaunchSettingsJson(csProjFolderPath, projectName);
    }
    
    private bool GenerateLaunchSettingsJson(string csProjFolderPath, string projectName)
    {
        string template = ReadToolFile(LaunchSettingsJsonTemplatePath);
        int iisHttpPort = _random.Next(50000, 55000);
        int iisHttpsPort = iisHttpPort + 1;
        
        int kestralHttpPort = _random.Next(10000, 15000);
        int kestralHttpsPort = kestralHttpPort+1;
        
        template = template.Replace("{iisExpress_applicationUrl_port}", iisHttpPort.ToString());
        template = template.Replace("{iisExpress_sslPort}", iisHttpsPort.ToString());
        
        template = template.Replace("{profileName}", projectName);
        
        template = template.Replace("{profileHttpPort}", kestralHttpPort.ToString());
        template = template.Replace("{profileHttpsPort}", kestralHttpsPort.ToString());

        string outputFilePath = Path.Combine(csProjFolderPath, LaunchSettingsJsonOutputPath);
        string? outputFileDirectory = Path.GetDirectoryName(outputFilePath);

        if (string.IsNullOrEmpty(outputFilePath))
        {
            return false;
        }

        if (!Directory.Exists(outputFileDirectory))
        {
            Directory.CreateDirectory(outputFileDirectory!);
        }
        
        File.WriteAllText(outputFilePath, template);
        return true;
    }
}