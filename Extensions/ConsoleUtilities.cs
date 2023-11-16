using System.Diagnostics;
using System.Drawing;
using BetterConsoles.Tables;
using BetterConsoles.Tables.Builders;
using BetterConsoles.Tables.Configuration;
using BetterConsoles.Tables.Models;
using Nexus.Runners;

namespace Nexus.Extensions;

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
        Table? tokenTable = new TableBuilder()
            .AddColumn("Service", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Constants.Colors.Info,
            }, rowsFormat: new CellFormat { ForegroundColor = Constants.Colors.Info })
            .AddColumn("Token", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Color.Gold,
            }, rowsFormat: new CellFormat { ForegroundColor = Color.Gold })
            .Build();
        
        tokenTable.AddRow("Consul", state.GlobalToken);
        foreach (KeyValuePair<string, string> serviceToken in state.ServiceTokens)
        {
            tokenTable.AddRow(serviceToken.Key, serviceToken.Value);
        }
        tokenTable.Config = TableConfig.Unicode();
        
        Table? servicesTable = new TableBuilder()
            .AddColumn("Service", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Constants.Colors.Info,
            }, rowsFormat: new CellFormat { ForegroundColor = Constants.Colors.Info })
            .AddColumn("Url", headerFormat: new CellFormat
            {
                Alignment = Alignment.Left,
                ForegroundColor = Color.Gold,
            }, rowsFormat: new CellFormat { ForegroundColor = Color.Gold })
            .Build();
        servicesTable.Config = TableConfig.Unicode();

        servicesTable.AddRow("Consul", "http://localhost:8500");
        servicesTable.AddRow("Frontend App", "http://localhost:3000");
        servicesTable.AddRow("Jaeger", "http://localhost:16686");
        servicesTable.AddRow("Kibana", "http://localhost:5601");
        servicesTable.AddRow("Prometheus", "http://localhost:9090");
        servicesTable.AddRow("Grafana", "http://localhost:3900");
        
        Console.WriteLine(tokenTable.ToString());
        Console.WriteLine(servicesTable.ToString());
    }
}