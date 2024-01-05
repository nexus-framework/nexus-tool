using System.Diagnostics;
using System.Text;
using Nexus.Runners;
using Spectre.Console;

namespace Nexus.Extensions;

public static class ConsoleUtilities
{
    public static string RunPowershellCommand(string command)
    {
        bool captureOutput = true;
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = command;
            
            if (captureOutput)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }
            
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;

            process.Start();

            if (captureOutput)
            {
                output = process.StandardOutput.ReadToEnd().Trim();
            }
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }

        return output;
    }
    
    public static string RunDockerCommand(string command)
    {
        bool captureOutput = true;
        Process process = new ();
        string output = string.Empty;
        try
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = command;

            if (captureOutput)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }
            
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = false;
            process.Start();

            if (captureOutput)
            {
                output = process.StandardOutput.ReadToEnd().Trim();
            }
        }
        finally
        {
            process.WaitForExit();
            process.Close();
        }
        return output;
    }
    
    public static string RunDockerCommandV2(string command)
    {
        bool captureOutput = true;
        Process process = new ();
        process.StartInfo.FileName = "docker";
        process.StartInfo.Arguments = command;

        if (captureOutput)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
        }
        
        process.StartInfo.UseShellExecute = false;
        process.EnableRaisingEvents = false;
        
        StringBuilder outputBuilder = new ();
        StringBuilder errorBuilder = new ();
        
        using (AutoResetEvent outputWaitHandle = new (false))
        using (AutoResetEvent errorWaitHandle = new (false))
        {
            if (captureOutput)
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };
            }
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(10 * 60 * 1000);
            outputWaitHandle.WaitOne(10 * 60 * 1000);
            errorWaitHandle.WaitOne(10 * 60 * 1000);
        }
        string output = outputBuilder.ToString().Trim();
        string error = errorBuilder.ToString().Trim();

        return output;
    }

    public static void PrintState(RunState state, RunType runType)
    {
        Table table = new ();
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[bold]Service[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Url[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Token[/]").LeftAligned());
        
        foreach (KeyValuePair<string, string> serviceToken in state.ServiceTokens)
        {
            table.AddRow(serviceToken.Key, "[cyan]N/A[/]", $"[yellow]{serviceToken.Value}[/]");
        }

        table.AddRow("Consul", $"[cyan]http://localhost:{GetServicePort("consul", runType)}[/]", $"[yellow]{state.GlobalToken}[/]");
        table.AddRow("Frontend App", $"[cyan]http://localhost:{GetServicePort("fe", runType)}[/]", "[yellow]N/A[/]");
        table.AddRow("Grafana", $"[cyan]http://localhost:{GetServicePort("grafana", runType)}[/]", "[yellow]N/A[/]");
        table.AddRow("Jaeger", $"[cyan]http://localhost:{GetServicePort("jaeger",runType)}[/]", "[yellow]N/A[/]");
        table.AddRow("Prometheus", $"[cyan]http://localhost:{GetServicePort("prometheus", runType)}[/]", "[yellow]N/A[/]");
        
        AnsiConsole.Write(table);
    }

    private static string GetServicePort(string service, RunType runType)
    {
        return runType switch
        {
            RunType.K8s => service switch
            {
                "consul" => "8500",
                "fe" => "3000",
                "grafana" => "3090",
                "jaeger" => "16687",
                "prometheus"=> "9091",
                _ => "",
            },
            _ => service switch
            {
                "consul" => "8500",
                "fe" => "3000",
                "grafana" => "3900",
                "jaeger" => "16686",
                "prometheus"=> "9090",
                _ => "",
            },
        };
    }

    public static void PrintVersion(RunState state)
    {
        if (!string.IsNullOrEmpty(state.DockerImageVersion))
        {
            AnsiConsole.MarkupLine($"Build Version: [cyan]{state.DockerImageVersion}[/]");
        }
    }
}