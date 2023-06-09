using Nexus.Config;
using Nexus.Extensions;
using Nexus.Models;
using Nexus.Services;
using static Nexus.Extensions.ConsoleUtilities;
using static Nexus.Extensions.DirectoryExtensions;

namespace Nexus.Generators;

public class SolutionGenerator
{
    private readonly ConfigurationService _configurationService;
    private readonly GitHubService _gitHubService;
    private readonly Random _random;

    public SolutionGenerator()
    {
        _configurationService = new ConfigurationService();
        _gitHubService = new GitHubService();
        _random = new Random();
    }

    public bool InitializeSolution(string rawName)
    {
        // create solution file
        string solutionName = NameExtensions.GetKebabCasedNameWithoutApi(rawName);
        bool solutionCreated = CreateSolutionFile(solutionName);
        if (!solutionCreated)
        {
            return false;
        }
        
        // add config
        bool configInitialized = _configurationService.InitializeConfig(rawName);
        if (!configInitialized)
        {
            return false;
        }
        
        // ensure services folder
        EnsureServicesFolder();
        
        // add api gateway
        
        
        // add hc dashboard
        // add discovery server
        
        // Prerequisites:
        // * Library Packages to be published to nuget
        // * API Gateway/HealthCheck Dashboard published as templates/nuget packages
        return true;
    }
    
    public async Task<bool> AddService(string rawName)
    {
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            return false;
        }

        int httpsPort = _random.Next(6000, 6100);

        ServiceInitializationInfo info = new (
            solutionName: config.ProjectName,
            serviceNameRaw: rawName,
            basePath: _configurationService.GetBasePath(),
            httpsPort: httpsPort,
            httpPort: httpsPort + 1);
        
        if (_configurationService.ServiceExists(info.ServiceNameKebabCaseAndApi))
        { 
            return false;
        }
        
        // Create service folders
        EnsureDirectories(new[] { info.ServiceRootFolder, info.ServiceCsProjectFolder });

        // Download project template
        await _gitHubService.DownloadServiceTemplate(info.ServiceCsProjectFolder);
        
        // Replace variables
        ReplaceTemplateVariables(info);

        // AddEnvironmentVariables();
        
        // Add service to solution
        AddServiceCsProjectFileToSolution(info.SolutionPath, info.ServiceCsProjectFile);
        return _configurationService.AddService(info);
    }

    private void ReplaceTemplateVariables(ServiceInitializationInfo info)
    {
        // List files
        var files = Directory.GetFiles(info.ServiceCsProjectFolder, "*.*", SearchOption.AllDirectories);

        // Replace variables
        foreach (string file in files)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Unable to find file {file}");
                continue;
            }
            
            var fileText = File.ReadAllText(file);
            var updatedText = fileText
                .Replace("{{RootNamespace}}", info.RootNamespace)
                .Replace("{{ServiceName}}", info.ServiceNameKebabCase) // FIXXXXX
                .Replace("{{ProjectName}}", info.ServiceNamePascalCasedAndDotApi)
                .Replace("{{ApplicationPort_Https}}", info.HttpsPort.ToString())
                .Replace("{{ApplicationPort_Http}}", info.HttpPort.ToString())
                .Replace("{{SolutionNameSnakeCase}}", info.SolutionNameSnakeCase)
                .Replace("{{ServiceNameSnakeCase}}", info.ServiceNameSnakeCase);
            
            File.WriteAllText(file, updatedText);
        }
        
        // Rename csproj
        var csProjFile = files.FirstOrDefault(x => x.EndsWith("ServiceTemplate.csproj"));
        if (csProjFile != null)
        {
            var updatedName = csProjFile.Replace("ServiceTemplate.csproj", $"{info.ServiceNamePascalCasedAndDotApi}.csproj");
            File.Move(csProjFile, updatedName);
        }
    }



    private bool CreateSolutionFile(string solutionName)
    {
        string slnFolder = _configurationService.GetBasePath(); 
        RunPowershellCommand($"dotnet new sln --output \"{slnFolder}\" --name {solutionName}");
        Console.WriteLine("Solution generated successfully!");
        return true;
    }

    private static bool AddServiceCsProjectFileToSolution(string solutionPath, string csProjectFilePath)
    {
        if (!File.Exists(csProjectFilePath))
        {
            // TODO: Write message
            return false;
        }

        RunPowershellCommand($"dotnet sln \"{solutionPath}\" add \"{csProjectFilePath}\"");
        return true;
    }
    
    private void EnsureServicesFolder()
    {
        var servicesFolderPath = Path.Combine(_configurationService.GetBasePath(), "services");
        if (!Directory.Exists(servicesFolderPath))
        {
            Directory.CreateDirectory(servicesFolderPath);
        }
    }
}