using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SaveData1.Helpers
{
    /// <summary>Сохраняет/восстанавливает размер и положение форм в %LOCALAPPDATA%/SaveData1/window-state.ini.</summary>
    public static class WindowStateStore
    {
        private static readonly object _lock = new object();

        private static string StoreFile
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveData1");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "window-state.ini");
            }
        }

        /// <summary>Привязать форму к сохранённым координатам: восстановить в Load и сохранить при закрытии.</summary>
        public static void Attach(Form form, string key = null)
        {
            if (form == null) return;
            string actualKey = string.IsNullOrWhiteSpace(key) ? form.GetType().FullName : key;
            form.Load += (s, e) => Restore(form, actualKey);
            form.FormClosing += (s, e) => Save(form, actualKey);
        }

        private static Dictionary<string, string> Read()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(StoreFile)) return dict;
                foreach (var line in File.ReadAllLines(StoreFile, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    dict[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
                }
            }
            catch (Exception ex) { AppLog.Warn("WindowStateStore.Read: " + ex.Message); }
            return dict;
        }

        private static void Write(Dictionary<string, string> dict)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var kv in dict)
                    sb.Append(kv.Key).Append('=').Append(kv.Value).AppendLine();
                File.WriteAllText(StoreFile, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex) { AppLog.Warn("WindowStateStore.Write: " + ex.Message); }
        }

        private static void Restore(Form form, string key)
        {
            lock (_lock)
            {
                var d = Read();
                if (!d.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value)) return;
                var parts = value.Split('|');
                if (parts.Length != 5) return;
                if (!int.TryParse(parts[0], out int x)) return;
                if (!int.TryParse(parts[1], out int y)) return;
                if (!int.TryParse(parts[2], out int w)) return;
                if (!int.TryParse(parts[3], out int h)) return;
                if (!Enum.TryParse(parts[4], out FormWindowState state)) state = FormWindowState.Normal;

                bool onScreen = false;
                foreach (var scr in Screen.AllScreens)
                {
                    if (scr.WorkingArea.IntersectsWith(new System.Drawing.Rectangle(x, y, w, h))) { onScreen = true; break; }
                }
                if (!onScreen) return;
                if (w < 400 || h < 300) return;

                form.StartPosition = FormStartPosition.Manual;
                form.Bounds = new System.Drawing.Rectangle(x, y, w, h);
                form.WindowState = state == FormWindowState.Minimized ? FormWindowState.Normal : state;
            }
        }

        private static void Save(Form form, string key)
        {
            lock (_lock)
            {
                try
                {
                    var bounds = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
                    var state = form.WindowState == FormWindowState.Minimized ? FormWindowState.Normal : form.WindowState;
                    string value = $"{bounds.X}|{bounds.Y}|{bounds.Width}|{bounds.Height}|{state}";
                    var d = Read();
                    d[key] = value;
                    Write(d);
                }
                catch (Exception ex) { AppLog.Warn("WindowStateStore.Save: " + ex.Message); }
            }
        }
    }
}
