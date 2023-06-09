namespace Nexus.Extensions;

public static class DirectoryExtensions
{
    public static void EnsureDirectories(string[] directories)
    {
        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}