using System.Reflection;

namespace Nexus.Generators;

public abstract class ProjectCodeGenerator
{
    public abstract bool GenerateFiles(string csProjFolderPath, string projectName, string serviceName);
    
    protected static string ReadToolFile(string path)
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        string? assemblyDirectory = Path.GetDirectoryName(assemblyPath);

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            return string.Empty;
        }
        
        string fullPath = Path.Combine(assemblyDirectory, path);
        return File.ReadAllText(fullPath);
    }

    protected static bool WriteFile(string path, string textToWrite)
    {
        string? outputFileDirectory = Path.GetDirectoryName(path);

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (!Directory.Exists(outputFileDirectory))
        {
            Directory.CreateDirectory(outputFileDirectory!);
        }
        
        File.WriteAllText(path, textToWrite);

        return true;
    }
}