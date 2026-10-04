using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

internal static class SharedBrightnessApp
{
    [STAThread]
    private static int Main(string[] args)
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        string log = Path.Combine(folder, "brightness.log");
        try
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(Path.Combine(folder, "settings.ini")))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) throw new FormatException("Invalid configuration line: " + line);
                values.Add(line.Substring(0, separator).Trim(), line.Substring(separator + 1).Trim());
            }
            TimeSpan day = DateTime.ParseExact(values["DayTime"], "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
            TimeSpan night = DateTime.ParseExact(values["NightTime"], "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
            int dayBrightness = ParseBrightness(values["DayBrightness"]);
            int nightBrightness = ParseBrightness(values["NightBrightness"]);
            if (day >= night) throw new FormatException("DayTime must be earlier than NightTime.");
            DateTime now = DateTime.Now;
            int target = now.TimeOfDay >= day && now.TimeOfDay < night ? dayBrightness : nightBrightness;
            string prefix = now.ToString("yyyy-MM-dd HH:mm:ss") + " target=" + target + "% ";
            // Validation mode does not access or change monitors.
            if (args.Length == 1 && args[0] == "--validate")
            {
                AppendLog(log, new[] { prefix + "Configuration valid." });
                return 0;
            }
            string[] results = AutoMonitorBrightness.Apply(target);
            var messages = new List<string>();
            bool failed = false;
            foreach (string result in results)
            {
                messages.Add(prefix + result);
                if (result.StartsWith("ERROR:", StringComparison.Ordinal)) failed = true;
            }
            AppendLog(log, messages);
            return failed ? 1 : 0;
        }
        catch (Exception error)
        {
            try { AppendLog(log, new[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ERROR: " + error.Message }); }
            catch { }
            return 1;
        }
    }

    private static int ParseBrightness(string value)
    {
        int result = int.Parse(value, CultureInfo.InvariantCulture);
        if (result < 0 || result > 100) throw new FormatException("Brightness must be 0 to 100.");
        return result;
    }

    private static void AppendLog(string path, IEnumerable<string> lines)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 1048576)
        {
            string[] oldLines = File.ReadAllLines(path);
            int count = Math.Min(200, oldLines.Length);
            var tail = new string[count];
            Array.Copy(oldLines, oldLines.Length - count, tail, 0, count);
            File.WriteAllLines(path, tail);
        }
        File.AppendAllLines(path, lines);
    }
}
