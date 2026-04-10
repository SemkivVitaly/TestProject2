using System;
using System.Collections.Generic;
using System.IO;
namespace SaveData1.Helpers
{
    public static class StandsStateHelper
    {
        public const string PoletnikiStateFileName = "stands_state_autotesting_poletniki.txt";

        public static string GetStateFilePath(string fileName = null)
        {
            string name = string.IsNullOrEmpty(fileName) ? PoletnikiStateFileName : fileName;
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(exeDir))
                exeDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(exeDir, name);
        }

        public class StandState
        {
            public string StandNumber { get; set; }
            public string VolumeSerialNumber { get; set; }
            public string VolumeLabel { get; set; }
            public string SerialNumber { get; set; }
        }

        public static void SaveState(IEnumerable<StandState> stands, string fileName = null)
        {
            string path = GetStateFilePath(fileName);
            var lines = new List<string>();
            foreach (var s in stands)
            {
                if (s == null) continue;
                string stand = (s.StandNumber ?? "").Replace("|", "_").Replace("\r", "").Replace("\n", "");
                string vol = (s.VolumeSerialNumber ?? "").Replace("|", "_").Replace("\r", "").Replace("\n", "");
                string label = (s.VolumeLabel ?? "").Replace("|", "_").Replace("\r", "").Replace("\n", "");
                string serial = (s.SerialNumber ?? "").Replace("|", "_").Replace("\r", "").Replace("\n", "");
                lines.Add($"{stand}|{vol}|{serial}|{label}");
            }
            try
            {
                File.WriteAllLines(path, lines);
            }
            catch { }
        }

        public static List<StandState> LoadState(string fileName = null)
        {
            string path = GetStateFilePath(fileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return new List<StandState>();

            try
            {
                var lines = File.ReadAllLines(path);
                var result = new List<StandState>();
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { '|' }, 4);
                    result.Add(new StandState
                    {
                        StandNumber = parts.Length > 0 ? parts[0] : "",
                        VolumeSerialNumber = parts.Length > 1 ? parts[1] : "",
                        SerialNumber = parts.Length > 2 ? parts[2] : "",
                        VolumeLabel = parts.Length > 3 ? parts[3] : ""
                    });
                }
                return result;
            }
            catch
            {
                return new List<StandState>();
            }
        }
    }
}
