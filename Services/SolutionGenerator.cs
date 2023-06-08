using CaseExtensions;
using Nexus.Config;
using static Nexus.Services.ConsoleUtilities;

namespace Nexus.Services;

public static class Constants
{
    public const string ServicesDirectory = "services";
}

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
        string solutionName = Utilities.GetKebabCasedNameWithoutApi(rawName);
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
        // serviceName: project-api
        // serviceRootFolder: services/project-api
        // serviceCsProjectName: Project.Api
        // serviceCsProjFolder: services/src/Project.Api
        // serviceCsProjFile: services/src/Project.Api/Project.Api.csproj
        NexusSolutionConfiguration? config = _configurationService.ReadConfiguration();

        if (config == null)
        {
            return false;
        }
        
        string solutionName = Utilities.GetKebabCasedNameWithoutApi(config.ProjectName);
        string solutionPath = Path.Combine(_configurationService.GetBasePath(), $"{solutionName}.sln");
        string kebabCasedNameAndApi = Utilities.GetKebabCasedNameAndApi(rawName);
        string serviceRootFolder = Path.Combine(_configurationService.GetBasePath(), Constants.ServicesDirectory, kebabCasedNameAndApi);
        string pascalCasedNameAndDotApi = Utilities.GetPascalCasedNameAndDotApi(rawName);
        string rootNamespace = pascalCasedNameAndDotApi.Replace(".", "");
        string serviceCsProjFolder = Path.Combine(serviceRootFolder, "src", pascalCasedNameAndDotApi);
        string serviceCsProjFile = Path.Combine(serviceCsProjFolder, $"{pascalCasedNameAndDotApi}.csproj");
        int httpsPort = _random.Next(6000, 6100);
        int httpPort = httpsPort + 1;
        
        if (_configurationService.ServiceExists(kebabCasedNameAndApi))
        { 
            return false;
        }
        
        // Create service folders
        // TODO: Add tests folder
        EnsureDirectories(new[] { serviceRootFolder, serviceCsProjFolder });

        // Download project template
        await _gitHubService.DownloadServiceTemplate(serviceCsProjFolder);
        // Replace variables
        ReplaceTemplateVariables(config.ProjectName, serviceCsProjFolder, pascalCasedNameAndDotApi, rootNamespace, kebabCasedNameAndApi, httpsPort, httpPort);

        // Add service to solution
        AddServiceCsProjectFileToSolution(solutionPath, serviceCsProjFile);
        
        return _configurationService.AddService(kebabCasedNameAndApi, pascalCasedNameAndDotApi, rootNamespace, httpsPort);
    }

    private void ReplaceTemplateVariables(
        string solutionName,
        string serviceCsProjFolder,
        string projectName, 
        string rootNamespace,
        string serviceName, 
        int httpPort,
        int httpsPort)
    {
        // List files
        var files = Directory.GetFiles(serviceCsProjFolder, "*.*", SearchOption.AllDirectories);
        var solutionNameSnakeCase = solutionName.ToSnakeCase();
        var serviceNameSnakeCase = serviceName.ToSnakeCase();
        
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
                .Replace("{{RootNamespace}}", rootNamespace)
                .Replace("{{ServiceName}}", serviceName)
                .Replace("{{ProjectName}}", projectName)
                .Replace("{{ApplicationPort_Https}}", httpsPort.ToString())
                .Replace("{{ApplicationPort_Http}}", httpPort.ToString())
                .Replace("{{SolutionNameSnakeCase}}", solutionNameSnakeCase)
                .Replace("{{ServiceNameSnakeCase}}", serviceNameSnakeCase);
            
            File.WriteAllText(file, updatedText);
        }
        
        // Rename csproj
        var csProjFile = files.FirstOrDefault(x => x.EndsWith("ServiceTemplate.csproj"));
        if (csProjFile != null)
        {
            var updatedName = csProjFile.Replace("ServiceTemplate.csproj", $"{projectName}.csproj");
            File.Move(csProjFile, updatedName);
        }
    }

    private static void EnsureDirectories(string[] directories)
    {
        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
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
        var servicesFolderPath = Path.Combine(_configurationService.GetBasePath(), Constants.ServicesDirectory);
        if (!Directory.Exists(servicesFolderPath))
        {
            Directory.CreateDirectory(servicesFolderPath);
        }
    }
}