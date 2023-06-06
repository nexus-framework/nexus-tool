using System.Diagnostics;

namespace Nexus.Services;

public static class ConsoleUtilities
{
    public static string RunPowershellCommand(string command)
    {
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;

            process.Start();

            output = process.StandardOutput.ReadToEnd();
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }

        return output;
    }
    
    public static string RunDockerCommand(string command, bool redirectStandard = true)
    {
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = command;

            if (redirectStandard)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }
            
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;

            process.Start();

            if (redirectStandard)
            {
                output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd();
            }
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }

        return output;
    }
}