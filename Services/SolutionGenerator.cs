using System.Diagnostics;
using System.Xml.Linq;
using Nexus.Generators;
using Nexus.Generators.LaunchSettings;

namespace Nexus.Services;

public static class Constants
{
    public const string SERVICES_DIRECTORY = "services";
}

public class SolutionGenerator
{
    private readonly ConfigurationService _configurationService;

    public SolutionGenerator()
    {
        _configurationService = new ConfigurationService();
    }

    public bool InitializeSolution(string rawName)
    {
        // create solution file
        bool solutionCreated = CreateSolutionFile(rawName);
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
    
    public bool AddService(string rawName)
    {
        // serviceName: project-api
        // serviceRootFolder: services/project-api
        // serviceCsProjectName: Project.Api
        // serviceCsProjFolder: services/src/Project.Api
        // serviceCsProjFile: services/src/Project.Api/Project.Api.csproj

        string kebabCasedNameAndApi = Utilities.GetKebabCasedNameAndApi(rawName);
        string serviceRootFolder = Path.Combine(Constants.SERVICES_DIRECTORY, kebabCasedNameAndApi);
        string pascalCasedNameAndDotApi = Utilities.GetPascalCasedNameAndDotApi(rawName);
        string serviceCsProjFolder = Path.Combine(serviceRootFolder, "src", pascalCasedNameAndDotApi);
        string serviceCsProjFile = Path.Combine(serviceCsProjFolder, $"{pascalCasedNameAndDotApi}.csproj");
        
        if (_configurationService.ServiceExists(kebabCasedNameAndApi))
        { 
            return false;
        }
        
        // Create service folders
        // TODO: Add tests folder
        EnsureDirectories(new[] { serviceRootFolder, serviceCsProjFolder });

        // Create project
        string csprojXml = GetProjectXml(pascalCasedNameAndDotApi);
        File.WriteAllText(serviceCsProjFile, csprojXml);

        // Add components based on some config
        
        // Add service to solution
        AddServiceCsProjectFileToSolution(serviceCsProjFile);
        AddGeneratedFilesToService(serviceCsProjFolder, pascalCasedNameAndDotApi, kebabCasedNameAndApi);
        
        return _configurationService.AddService(kebabCasedNameAndApi);
    }

    private static bool AddGeneratedFilesToService(string csProjFolderPath, string projectName, string serviceName)
    {
        ProjectCodeGenerator launchSettingsGenerator = new LaunchSettingsGenerator();
        ProjectCodeGenerator consulCodeGenerator = new ConsulCodeGenerator();

        launchSettingsGenerator.GenerateFiles(csProjFolderPath, projectName, serviceName);
        consulCodeGenerator.GenerateFiles(csProjFolderPath, projectName, serviceName);
        
        return true;
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

    private static string GetProjectXml(string rootNamespace)
    {
        XElement propertyGroup = new XElement("PropertyGroup",
            new XElement("TargetFramework", "net7.0"),
            new XElement("Nullable", "enable"),
            new XElement("ImplicitUsings", "enable"),
            new XElement("RootNamespace", rootNamespace),
            new XElement("GenerateDocumentationFile", "true"),
            new XElement("NoWarn", "$(NoWarn);1591"),
            new XElement("DockerDefaultTargetOS", "Linux"),
            new XElement("GenerateAssemblyInfo", "false"),
            new XElement("GenerateTargetFrameworkAttribute", "false")
        );
  
        XElement projectElement = new XElement("Project", propertyGroup,
            new XAttribute("Sdk", "Microsoft.NET.Sdk.Web"));
        
        XDocument document = new XDocument(projectElement);
        
        return document.ToString();
    }

    private static bool CreateSolutionFile(string rawName)
    {
        string kebabCasedNameWithoutApi = Utilities.GetKebabCasedNameWithoutApi(rawName);
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new sln -n {kebabCasedNameWithoutApi}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process? process = Process.Start(startInfo);

        process?.WaitForExit();

        if (process?.ExitCode == 0)
        {
            Console.WriteLine("Solution generated successfully!");
            return true;
        }

        string errorMessage = process?.StandardError.ReadToEnd() ?? "Unknown error";
        Console.WriteLine($"Solution generation failed. Error message: {errorMessage}");
        return false;
    }

    private static bool AddServiceCsProjectFileToSolution(string csProjectFilePath)
    {
        if (!File.Exists(csProjectFilePath))
        {
            // TODO: Write message
            return false;
        }
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"sln add {csProjectFilePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process? process = Process.Start(startInfo);

        process?.WaitForExit();

        if (process?.ExitCode == 0)
        {
            Console.WriteLine("Service added to solution");
            return true;
        }
        
        string errorMessage = process?.StandardError.ReadToEnd() ?? "Unknown error";
        Console.WriteLine($"Service could not be added to solution. Error message: {errorMessage}");
        return false;
    }
    
    private static void EnsureServicesFolder()
    {
        if (!Directory.Exists(Constants.SERVICES_DIRECTORY))
        {
            Directory.CreateDirectory(Constants.SERVICES_DIRECTORY);
        }
    }
}