using System;
using System.Web.Script.Serialization;
using SaveData1.CrossPlateTesting.Models;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.CrossPlateTesting.Services
{
    public static class CrossPlateConfigService
    {
        public static string ConfigKey(string actNumber) => "CrossPlateCfg_" + (actNumber ?? "").Trim();

        public static AppConfig Load(SaveDataEntities2 ctx, string actNumber)
        {
            try
            {
                string json = ctx.GetSavePathForAct(ConfigKey(actNumber));
                if (string.IsNullOrWhiteSpace(json)) return new AppConfig();
                var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                var cfg = ser.Deserialize<AppConfig>(json) ?? new AppConfig();
                UnprotectStandWifiPasswords(cfg);
                return cfg;
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(SaveDataEntities2 ctx, string actNumber, AppConfig config)
        {
            var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var forDisk = CloneForEncryptedStorage(config, ser);
            string json = ser.Serialize(forDisk);
            ctx.SetSavePathForAct(ConfigKey(actNumber), json);
        }

        private static void UnprotectStandWifiPasswords(AppConfig cfg)
        {
            if (cfg?.Stands == null) return;
            foreach (var s in cfg.Stands)
                s.WifiPassword = LocalSecretProtector.UnprotectOrPlain(s.WifiPassword ?? "");
        }

        /// <summary>Копия конфигурации с защищёнными паролями Wi‑Fi для записи в БД (в памяти исходный config не меняем).</summary>
        private static AppConfig CloneForEncryptedStorage(AppConfig src, JavaScriptSerializer ser)
        {
            if (src == null) return new AppConfig();
            var copy = ser.Deserialize<AppConfig>(ser.Serialize(src)) ?? new AppConfig();
            if (copy.Stands != null)
            {
                foreach (var s in copy.Stands)
                {
                    if (string.IsNullOrEmpty(s.WifiPassword)) continue;
                    if (LocalSecretProtector.IsProtected(s.WifiPassword)) continue;
                    s.WifiPassword = LocalSecretProtector.Protect(s.WifiPassword);
                }
            }
            return copy;
        }
    }
}
