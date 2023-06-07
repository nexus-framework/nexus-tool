using System.Diagnostics;
using System.Drawing;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using Nexus.Runners;

namespace Nexus.Services;

public static class ConsoleUtilities
{
    public static string RunPowershellCommand(string command, bool captureOutput = true)
    {
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
    
    public static string RunDockerCommand(string command, bool captureOutput = true)
    {
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

    public static void PrintState(RunState state)
    {
        var serviceTable = new TableBuilder()
            .AddColumn("Service", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Color.Cyan,
            }, rowsFormat: new CellFormat { ForegroundColor = Color.Cyan })
            .AddColumn("Token", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Color.Gold,
            }, rowsFormat: new CellFormat { ForegroundColor = Color.Gold })
            .AddColumn("Url", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Color.Gold,
            }, rowsFormat: new CellFormat { ForegroundColor = Color.Gold })
            .Build();
        
        
        serviceTable.AddRow("Consul", state.GlobalToken, "http://localhost:8500");
        foreach (var serviceToken in state.ServiceTokens)
        {
            state.ServiceUrls.TryGetValue(serviceToken.Key, out string? serviceUrl);
            serviceTable.AddRow(serviceToken.Key, serviceToken.Value, serviceUrl);
        }

        serviceTable.AddRow("Jaeger", "", "http://localhost:16686");
        serviceTable.AddRow("Kibana", "", "http://localhost:5601");
        serviceTable.AddRow("Prometheus", "", "http://localhost:9090");
        serviceTable.AddRow("Grafana", "", "http://localhost:3900");
        
        serviceTable.Config = TableConfig.Unicode();
        Console.WriteLine(serviceTable.ToString());
    }
}