using System;
using System.IO;

namespace SaveData1.Helpers
{
    /// <summary>Хранение настроек OBS (IP, порт, пароль) в файле рядом с .exe.</summary>
    public static class ObsConfig
    {
        private static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "obs_settings.cfg");

        /// <summary>Проверка наличия файла настроек.</summary>
        public static bool IsConfigured()
        {
            return File.Exists(ConfigPath);
        }

        /// <summary>Загрузка настроек из файла.</summary>
        public static void Load(out string ip, out int port, out string password)
        {
            ip = "127.0.0.1";
            port = 4455;
            password = "";

            if (!File.Exists(ConfigPath)) return;

            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;
                string key = parts[0].Trim().ToLowerInvariant();
                string val = parts[1].Trim();
                switch (key)
                {
                    case "ip": ip = val; break;
                    case "port": int.TryParse(val, out port); break;
                    case "password": password = LocalSecretProtector.UnprotectOrPlain(val); break;
                }
            }
        }

        /// <summary>Сохранение настроек в файл.</summary>
        public static void Save(string ip, int port, string password)
        {
            string storedPwd = string.IsNullOrEmpty(password) ? "" : LocalSecretProtector.Protect(password);
            File.WriteAllText(ConfigPath, $"ip={ip}\r\nport={port}\r\npassword={storedPwd}");
        }

        /// <summary>Удаление файла настроек.</summary>
        public static void Delete()
        {
            if (File.Exists(ConfigPath))
                File.Delete(ConfigPath);
        }
    }
}
