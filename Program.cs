using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

class Blackout
{
    static string folderPath = AppDomain.CurrentDomain.BaseDirectory;
    static string configFile = Path.Combine(folderPath, "blackout_time.txt");
    static string presetFile = Path.Combine(folderPath, "blackout_presets.txt");
    static string modeFile = Path.Combine(folderPath, "blackout_mode.txt");
    static string shutdownTaskName = "Blackout_Schedule";

    static void Main(string[] args)
    {
        if (args.Length == 0 || args[0].ToLower() == "help")
        {
            ShowHelp();
            return;
        }

        string cmd = args[0].ToLower();
        switch (cmd)
        {
            case "status":
                ShowStatus();
                break;
            case "set":
                if (args.Length > 1) SetTime(args[1]);
                else Console.WriteLine("[BLACKOUT] Missing time argument.");
                break;
            case "preset":
            case "ps":
                if (args.Length > 1) UsePreset(args[1]);
                else Console.WriteLine("[BLACKOUT] Missing preset name.");
                break;
            case "addpreset":
            case "addps":
                if (args.Length > 2) AddPreset(args[1], args[2]);
                else Console.WriteLine("[BLACKOUT] Usage: addpreset NAME HH:mm");
                break;
            case "editpreset":
            case "editps":
                if (args.Length > 2) EditPreset(args[1], args[2]);
                else Console.WriteLine("[BLACKOUT] Usage: editpreset NAME HH:mm");
                break;
            case "delpreset":
            case "delps":
                if (args.Length > 1) DeletePreset(args[1]);
                else Console.WriteLine("[BLACKOUT] Usage: delpreset NAME");
                break;
            case "listpresets":
            case "lsps":
                ListPresets();
                break;
            case "clear":
            case "clr":
                ClearShutdown();
                break;
            case "enable":
                EnableShutdown();
                break;
            case "disable":
                DisableShutdown();
                break;
            case "now":
                ExecuteShutdownOrHibernate();
                break;
            case "mode":
                if (args.Length > 1) SetMode(args[1]);
                else Console.WriteLine("[BLACKOUT] Usage: mode shutdown|hibernate");
                break;
            default:
                Console.WriteLine("[BLACKOUT] Unknown command.");
                ShowHelp();
                break;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
BLACKOUT - Professional Shutdown Scheduler CLI

Usage:
  blackout [command] [arguments]

Commands:
  status           Show shutdown status and mode
  now              Shutdown or hibernate immediately
  set HH:mm        Schedule shutdown or hibernate at specified time
  preset NAME      Apply a saved preset
  addpreset NAME HH:mm   Create a new preset
  editpreset NAME HH:mm  Edit an existing preset
  delpreset NAME         Delete a preset
  listpresets      List all saved presets
  clear | clr      Clear scheduled shutdown/hibernate
  enable           Enable scheduled task
  disable          Disable scheduled task
  mode [shutdown|hibernate]   Set or display current mode
  help             Show this help information

Aliases:
  clr    = clear
  lsps   = listpresets
  addps  = addpreset
  delps  = delpreset

Examples:
  blackout set 23:00
  blackout preset Normal_Shutdown
  blackout mode hibernate
");
    }


    // ---------------- Preset Management ----------------

    static void AddPreset(string name, string time)
    {
        if (!TimeSpan.TryParse(time.Trim(), out _))
        {
            Console.WriteLine("[BLACKOUT] Invalid time format. Use HH:mm");
            return;
        }

        var presets = LoadPresets();
        if (presets.ContainsKey(name))
        {
            Console.WriteLine("[BLACKOUT] Preset already exists. Use editpreset to change it.");
            return;
        }

        presets[name] = time.Trim();
        SavePresets(presets);
        Console.WriteLine($"[BLACKOUT] Preset '{name}' added with time {time}");
    }

    static void EditPreset(string name, string time)
    {
        if (!TimeSpan.TryParse(time.Trim(), out _))
        {
            Console.WriteLine("[BLACKOUT] Invalid time format. Use HH:mm");
            return;
        }

        var presets = LoadPresets();
        if (!presets.ContainsKey(name))
        {
            Console.WriteLine("[BLACKOUT] Preset not found. Use addpreset to create it.");
            return;
        }

        presets[name] = time.Trim();
        SavePresets(presets);
        Console.WriteLine($"[BLACKOUT] Preset '{name}' updated to {time}");
    }

    static void DeletePreset(string name)
    {
        var presets = LoadPresets();
        if (!presets.ContainsKey(name))
        {
            Console.WriteLine("[BLACKOUT] Preset not found.");
            return;
        }

        presets.Remove(name);
        SavePresets(presets);
        Console.WriteLine($"[BLACKOUT] Preset '{name}' deleted.");
    }

    static void UsePreset(string name)
    {
        var presets = LoadPresets();
        if (!presets.ContainsKey(name))
        {
            Console.WriteLine("[BLACKOUT] Preset not found.");
            return;
        }

        string time = presets[name];
        File.WriteAllText(configFile, time);
        CreateOrUpdateShutdownTask(time);
        Console.WriteLine($"[BLACKOUT] Preset '{name}' applied: {time}");
    }

    static void ListPresets()
    {
        var presets = LoadPresets();
        if (presets.Count == 0)
        {
            Console.WriteLine("[BLACKOUT] No presets saved.");
            return;
        }

        Console.WriteLine("[BLACKOUT] Saved Presets:");
        foreach (var kv in presets)
            Console.WriteLine($"  {kv.Key} -> {kv.Value}");
    }

    static Dictionary<string, string> LoadPresets()
    {
        var dict = new Dictionary<string, string>();
        if (!File.Exists(presetFile)) return dict;

        foreach (var line in File.ReadAllLines(presetFile))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('|');
            if (parts.Length == 2) dict[parts[0].Trim()] = parts[1].Trim();
        }
        return dict;
    }

    static void SavePresets(Dictionary<string, string> presets)
    {
        using (var writer = new StreamWriter(presetFile))
            foreach (var kv in presets)
                writer.WriteLine($"{kv.Key}|{kv.Value}");
    }

    // ---------------- Mode Management ----------------

    static void SetMode(string mode)
    {
        mode = mode.Trim().ToLower();
        if (mode != "shutdown" && mode != "hibernate")
        {
            Console.WriteLine("[BLACKOUT] Invalid mode. Use 'shutdown' or 'hibernate'.");
            return;
        }

        if (mode == "hibernate" && !SystemSupportsHibernate())
        {
            Console.WriteLine("[BLACKOUT] Hibernate is not enabled. Please enable it manually.");
            return;
        }

        File.WriteAllText(modeFile, mode);
        Console.WriteLine($"[BLACKOUT] Mode set to '{mode}'.");
    }

    static bool SystemSupportsHibernate()
    {
        Process p = new Process();
        p.StartInfo.FileName = "powercfg.exe";
        p.StartInfo.Arguments = "/a";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();

        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return output.Contains("Hibernate");
    }

    // ---------------- Shutdown & Schedule ----------------

    static void SetTime(string time)
    {
        if (!TimeSpan.TryParse(time.Trim(), out TimeSpan ts))
        {
            Console.WriteLine("[BLACKOUT] Invalid time format. Use HH:mm");
            return;
        }

        File.WriteAllText(configFile, time.Trim());
        CreateOrUpdateShutdownTask(time.Trim());
        Console.WriteLine($"[BLACKOUT] Scheduled at {time}");
    }

    static void ExecuteShutdownOrHibernate()
    {
        string mode = File.Exists(modeFile) ? File.ReadAllText(modeFile).Trim().ToLower() : "shutdown";

        if (mode == "hibernate")
        {
            if (!SystemSupportsHibernate())
            {
                Console.WriteLine("[BLACKOUT] Hibernate is not enabled. Cannot execute hibernate.");
                return;
            }
            Console.WriteLine("[BLACKOUT] Hibernating now...");
            RunCommand("shutdown.exe", "/h");
        }
        else
        {
            Console.WriteLine("[BLACKOUT] Shutting down now...");
            RunCommand("shutdown.exe", "/s /t 0");
        }
    }

    static void CreateOrUpdateShutdownTask(string time)
    {
        if (!TimeSpan.TryParse(time.Trim(), out TimeSpan ts))
        {
            Console.WriteLine("[BLACKOUT] Invalid time format for task.");
            return;
        }

        string exePath = Path.Combine(folderPath, "blackout.exe");
        string timeStr = ts.ToString(@"hh\:mm");

        if (IsTaskExist(shutdownTaskName)) RunSchtasks("/Delete", "/F", "/TN", shutdownTaskName);

        string trArg = $"\"\\\"{exePath}\\\" now\"";

        RunSchtasks("/Create", "/F", "/TN", shutdownTaskName,
            "/TR", trArg,
            "/SC", "DAILY",
            "/ST", timeStr,
            "/RL", "LIMITED");

        Console.WriteLine($"[BLACKOUT] Scheduled Task Created: {timeStr}");
    }

    static void ClearShutdown()
    {
        if (File.Exists(configFile)) File.Delete(configFile);
        if (IsTaskExist(shutdownTaskName)) RunSchtasks("/Delete", "/F", "/TN", shutdownTaskName);
        Console.WriteLine("[BLACKOUT] Scheduled shutdown cleared.");
    }

    static void EnableShutdown()
    {
        if (!File.Exists(configFile))
        {
            Console.WriteLine("[BLACKOUT] No scheduled shutdown to enable.");
            return;
        }
        string time = File.ReadAllText(configFile).Trim();
        CreateOrUpdateShutdownTask(time);
        Console.WriteLine($"[BLACKOUT] Shutdown task ENABLED at {time}");
    }

    static void DisableShutdown()
    {
        if (IsTaskExist(shutdownTaskName)) RunSchtasks("/Delete", "/F", "/TN", shutdownTaskName);
        Console.WriteLine("[BLACKOUT] Scheduled shutdown DISABLED.");
    }

    static void ShowStatus()
    {
        string time = File.Exists(configFile) ? File.ReadAllText(configFile).Trim() : "Not set";
        string mode = File.Exists(modeFile) ? File.ReadAllText(modeFile).Trim() : "shutdown";
        bool taskExists = IsTaskExist(shutdownTaskName);

        Console.WriteLine($"[BLACKOUT] Next action: {time} ({mode})");
        Console.WriteLine($"Task Exists: {taskExists}");

        if (TimeSpan.TryParse(time, out TimeSpan ts))
        {
            DateTime target = DateTime.Today.Add(ts);
            if (target < DateTime.Now) target = target.AddDays(1);
            TimeSpan remaining = target - DateTime.Now;
            Console.WriteLine($"Countdown: {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s");
        }
    }

    // ---------------- Utility ----------------

    static bool IsTaskExist(string taskName)
    {
        Process p = new Process();
        p.StartInfo.FileName = "schtasks";
        p.StartInfo.Arguments = $"/Query /TN \"{taskName}\"";
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return !output.Contains("ERROR:");
    }

    static void RunSchtasks(params string[] args) => RunCommand("schtasks.exe", string.Join(" ", args));

    static void RunCommand(string cmd, string args)
    {
        Process p = new Process();
        p.StartInfo.FileName = cmd;
        p.StartInfo.Arguments = args;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.Start();

        string output = p.StandardOutput.ReadToEnd();
        string error = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!string.IsNullOrEmpty(output)) Console.WriteLine("[DEBUG] " + output.Trim());
        if (!string.IsNullOrEmpty(error)) Console.WriteLine("[DEBUG] ERROR: " + error.Trim());
    }
}