using System.IO.Compression;
using System.Net;

namespace Nexus.Services;

public class GitHubService
{
    private const string TemplateUrl = @"https://codeload.github.com/afroze9/nexus-template/zip/refs/heads/master";
    
    public async Task DownloadServiceTemplate(string destPath)
    {
        // create temp folder
        var tempFolderPath = Path.Combine(Path.GetTempPath(), "nexus", Guid.NewGuid().ToString());
        var downloadFilePath = Path.Combine(tempFolderPath, "template.zip");
        var extractPath = Path.Combine(tempFolderPath, "template");
        var templateSourcePath = Path.Combine(extractPath, "nexus-template-master", "ServiceTemplate");

        try
        {
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }

            // download files to temp
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(TemplateUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                await using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    await using (var fileStream = new FileStream(downloadFilePath, FileMode.Create,
                                     FileAccess.ReadWrite,
                                     FileShare.ReadWrite))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }
                }
            }

            // Extract files
            if (File.Exists(downloadFilePath))
            {
                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                ZipFile.ExtractToDirectory(downloadFilePath, extractPath);
            }

            // move files to dest
            CopyDirectory(templateSourcePath, destPath, true);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            if (Directory.Exists(tempFolderPath))
            {
                Directory.Delete(tempFolderPath, true);
            }
        }
    }
    
    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}