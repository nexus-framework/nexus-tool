namespace Nexus.Generators;

public class ConsulCodeGenerator : ProjectCodeGenerator
{
    private readonly Random _random;
    private const string ConsulAppConfigJsonTemplatePath = "Templates/Consul/app-config.json.txt";
    private const string ConsulAppConfigJsonOutputPath = "Consul/app-config.json";
    
    private const string ConsulRulesTemplatePath = "Templates/Consul/rules.hcl.txt";
    private const string ConsulRulesOutputPath = "Consul/rules.hcl";

    public ConsulCodeGenerator()
    {
        _random = new Random();
    }
    
    public override bool GenerateFiles(string csProjFolderPath, string projectName, string serviceName)
    {
        GenerateAppConfig(csProjFolderPath, serviceName);
        GenerateRulesFile(csProjFolderPath, serviceName);
        return true;
    }

    private bool GenerateAppConfig(string csProjFolderPath, string serviceName)
    {
        string template = ReadToolFile(ConsulAppConfigJsonTemplatePath);
        int postgresPort = _random.Next(5400, 5500);
        string postgresDb = $"{serviceName}-db";
        string indexFormatServiceName = serviceName;

        template = template
            .Replace("{postgresPort}", postgresPort.ToString())
            .Replace("{postgresDb}", postgresDb)
            .Replace("{indexFormatServiceName}", indexFormatServiceName)
            .Replace("{serviceName}", serviceName);

        string outputFilePath = Path.Combine(csProjFolderPath, ConsulAppConfigJsonOutputPath);
        return WriteFile(outputFilePath, template);
    }

    private bool GenerateRulesFile(string csProjFolderPath, string serviceName)
    {
        string template = ReadToolFile(ConsulRulesTemplatePath);
        template = template
            .Replace("{serviceName}", serviceName);
        
        string outputFilePath = Path.Combine(csProjFolderPath, ConsulRulesOutputPath);
        return WriteFile(outputFilePath, template);
    }
}