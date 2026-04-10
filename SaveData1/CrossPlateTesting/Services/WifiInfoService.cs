using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Получение информации о текущем Wi-Fi подключении и списке доступных сетей
    /// </summary>
    public static class WifiInfoService
    {
        /// <summary>
        /// Проверяет, доступна ли указанная Wi‑Fi сеть (SSID в списке netsh).
        /// Использует findstr для быстрой проверки без полного парсинга вывода.
        /// </summary>
        public static bool IsNetworkAvailable(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid)) return false;
            try
            {
                string safe = ssid.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c \"netsh wlan show networks mode=bssid 2>nul | findstr {safe}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                using (var p = Process.Start(startInfo))
                {
                    string output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit(5000);
                    return !string.IsNullOrWhiteSpace(output);
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Возвращает список SSID доступных Wi-Fi сетей.
        /// </summary>
        public static List<string> GetAvailableNetworks()
        {
            var result = new List<string>();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show networks mode=bssid",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (var p = Process.Start(startInfo))
                {
                    string output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit(10000);

                    var matches = Regex.Matches(output, @"SSID\s+\d+\s*:\s*(.+)", RegexOptions.IgnoreCase);
                    foreach (Match m in matches)
                    {
                        if (m.Success)
                        {
                            var ssid = m.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(ssid) && ssid != "<сеть не транслируется>" && !result.Contains(ssid))
                                result.Add(ssid);
                        }
                    }
                    if (result.Count == 0)
                    {
                        var altMatches = Regex.Matches(output, @"SSID\s*:\s*(.+)", RegexOptions.IgnoreCase);
                        foreach (Match m in altMatches)
                        {
                            if (m.Success)
                            {
                                var ssid = m.Groups[1].Value.Trim();
                                if (!string.IsNullOrEmpty(ssid) && ssid != "<сеть не транслируется>" && !result.Contains(ssid))
                                    result.Add(ssid);
                            }
                        }
                    }
                }
            }
            catch { }
            return result;
        }
        public static (string Ssid, string Password) GetCurrentWifiInfo()
        {
            string ssid = GetCurrentSsid();
            if (string.IsNullOrEmpty(ssid))
                return (null, null);

            string password = GetWifiPassword(ssid);
            return (ssid, password);
        }

        public static string GetCurrentSsid()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (var p = Process.Start(startInfo))
                {
                    string output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit(5000);

                    var match = Regex.Match(output, @"SSID\s*:\s*(.+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }
            catch { }

            return null;
        }

        public static string GetWifiPassword(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid))
                return null;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan show profile name=\"{ssid}\" key=clear",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (var p = Process.Start(startInfo))
                {
                    string output = p?.StandardOutput.ReadToEnd() ?? "";
                    p?.WaitForExit(5000);

                    var match = Regex.Match(output, @"Key Content\s*:\s*(.+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
