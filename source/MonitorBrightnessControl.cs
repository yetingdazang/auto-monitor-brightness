using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class AutoMonitorBrightness {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PhysicalMonitor {
        public IntPtr Handle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Description;
    }
    public delegate bool MonitorCallback(IntPtr monitor, IntPtr dc, IntPtr rect, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorCallback callback, IntPtr data);
    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr monitor, out uint count);
    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr monitor, uint count, [Out] PhysicalMonitor[] monitors);
    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetMonitorBrightness(IntPtr monitor, out uint min, out uint current, out uint max);
    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool SetMonitorBrightness(IntPtr monitor, uint value);
    [DllImport("dxva2.dll")]
    static extern bool DestroyPhysicalMonitors(uint count, PhysicalMonitor[] monitors);

    public static string[] Apply(int percent) {
        if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException("percent");
        var results = new List<string>();
        MonitorCallback callback = (monitor, dc, rect, data) => {
            uint count;
            if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out count)) {
                results.Add("ERROR: enumerate monitor: " + Marshal.GetLastWin32Error());
                return true;
            }
            if (count == 0) return true;
            var monitors = new PhysicalMonitor[count];
            if (!GetPhysicalMonitorsFromHMONITOR(monitor, count, monitors)) {
                results.Add("ERROR: get physical monitors: " + Marshal.GetLastWin32Error());
                return true;
            }
            try {
                foreach (var item in monitors) {
                    uint min, current, max;
                    if (!GetMonitorBrightness(item.Handle, out min, out current, out max)) {
                        results.Add("ERROR: " + item.Description + ": read brightness: " + Marshal.GetLastWin32Error());
                        continue;
                    }
                    uint target = min + (uint)Math.Round(((double)max - min) * percent / 100.0);
                    if (current == target) {
                        results.Add(item.Description + ": already " + target + " (range " + min + "-" + max + ")");
                        continue;
                    }
                    if (!SetMonitorBrightness(item.Handle, target)) {
                        results.Add("ERROR: " + item.Description + ": set brightness: " + Marshal.GetLastWin32Error());
                        continue;
                    }
                    uint verified;
                    if (!GetMonitorBrightness(item.Handle, out min, out verified, out max) || verified != target) {
                        results.Add("ERROR: " + item.Description + ": brightness verification failed");
                    } else {
                        results.Add(item.Description + ": verified " + verified + " (range " + min + "-" + max + ")");
                    }
                }
            } finally { DestroyPhysicalMonitors(count, monitors); }
            return true;
        };
        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
            results.Add("ERROR: EnumDisplayMonitors: " + Marshal.GetLastWin32Error());
        if (results.Count == 0) results.Add("ERROR: no monitors available in this session");
        return results.ToArray();
    }
}
