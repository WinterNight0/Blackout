using System;
using System.IO;
using System.Diagnostics;

class Blackout
{
    static string configPath = @"C:\blackout_time.txt";
    static string taskName = "Blackout";

    static void Main(string[] args)
    {
        if (args.Length == 0 || args[0].ToLower() == "help")
        {
            ShowHelp();
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "set":
                SetTime(args);
                break;
            case "enable":
                EnableTask();
                break;
            case "disable":
                DisableTask();
                break;
            case "status":
                ShowStatus();
                break;
            case "cancel":
                CancelShutdown();
                break;
            case "now":
                ShutdownNow();
                break;
            default:
                Console.WriteLine("[BLACKOUT] Unknown command.\n");
                ShowHelp();
                break;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
[BLACKOUT] - System Shutdown Controller

Commands:
  set HH:mm   - Set shutdown time (24-hour format)
  enable      - Enable scheduled shutdown
  disable     - Disable scheduled shutdown
  status      - Show current shutdown status
  cancel      - Cancel any pending shutdown
  now         - Shutdown immediately
  help        - Display this help message

Examples:
  blackout set 23:45
  blackout enable
  blackout disable
  blackout status
  blackout cancel
  blackout now
");
    }

    static void SetTime(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("[BLACKOUT] Please specify a time in HH:mm format.");
            return;
        }

        if (!TimeSpan.TryParse(args[1], out TimeSpan _))
        {
            Console.WriteLine("[BLACKOUT] Invalid time format. Use HH:mm (e.g., 23:45).");
            return;
        }

        File.WriteAllText(configPath, args[1]);
        Console.WriteLine($"[BLACKOUT] Shutdown time set to {args[1]}.");
    }

    static void EnableTask()
    {
        RunSchtasks("/Change", "/Enable");
        Console.WriteLine("[BLACKOUT] Scheduled shutdown ENABLED.");
    }

    static void DisableTask()
    {
        RunSchtasks("/Change", "/Disable");
        Console.WriteLine("[BLACKOUT] Scheduled shutdown DISABLED.");
    }

    static void ShowStatus()
    {
        string time = File.Exists(configPath) ? File.ReadAllText(configPath).Trim() : "Not set";
        bool enabled = IsTaskEnabled();

        Console.WriteLine("[BLACKOUT] SYSTEM STATUS");
        Console.WriteLine($"Task Scheduler: {(enabled ? "ENABLED" : "DISABLED")}");
        Console.WriteLine($"Next Shutdown: {time}");
    }

    static void CancelShutdown()
    {
        RunCommand("shutdown", "/a");
        Console.WriteLine("[BLACKOUT] Pending shutdown cancelled.");
    }

    static void ShutdownNow()
    {
        Console.WriteLine("[BLACKOUT] Initiating immediate shutdown...");
        RunCommand("shutdown", "/s /t 0");
    }

    static void RunSchtasks(params string[] options)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/TN \"{taskName}\" {string.Join(" ", options)}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (Process p = Process.Start(psi))
        {
            p.WaitForExit();
        }
    }

    static bool IsTaskEnabled()
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Query /TN \"{taskName}\" /FO LIST /V",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process p = Process.Start(psi))
        {
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output.Contains("Ready"); // Ready means enabled
        }
    }

    static void RunCommand(string command, string args)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (Process p = Process.Start(psi))
        {
            p.WaitForExit();
        }
    }
}
