using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Скачивает единый текстовый лог с ESP32 DroneBridge (GET /api/log/file) и сохраняет
    /// рядом с Excel-отчётами: ExcelOutputFolder / {серийный}/ {серийный}.txt
    /// </summary>
    public static class BridgeUnifiedLogSaveService
    {
        /// <param name="excelOutputFolder">Базовый каталог отчётов; внутри создаётся подпапка по серийному номеру.</param>
        /// <param name="stand">Стенд; нужен непустой <see cref="Stand.ProductSerialNumber"/> или <see cref="Stand.Name"/>.</param>
        /// <param name="bridgeHost">Хост HTTP API моста (по умолчанию 192.168.2.1).</param>
        /// <param name="bridgePort">Порт (80 без суффикса в URL).</param>
        /// <param name="timeoutMs">Таймаут HTTP GET; фактически не ниже 3000 мс.</param>
        /// <param name="log">Колбэк для сообщений в UI; может быть null.</param>
        public static void TrySaveAfterStandTest(
            string excelOutputFolder,
            Stand stand,
            string bridgeHost,
            int bridgePort,
            int timeoutMs,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(excelOutputFolder) || stand == null)
                return;

            string serial = (stand.ProductSerialNumber ?? stand.Name ?? "").Trim();
            if (string.IsNullOrEmpty(serial))
            {
                log?.Invoke("[Лог моста] Пропуск: не задан серийный номер.");
                return;
            }

            string folderSafe = SanitizeForFileName(serial);
            if (string.IsNullOrEmpty(folderSafe))
                folderSafe = "SN_unknown";

            string host = (bridgeHost ?? "").Trim();
            if (string.IsNullOrEmpty(host))
                host = "192.168.2.1";
            if (bridgePort <= 0)
                bridgePort = 80;

            int t = Math.Max(3000, timeoutMs);

            string url = bridgePort == 80
                ? $"http://{host}/api/log/file"
                : $"http://{host}:{bridgePort}/api/log/file";

            try
            {
                string body;
                using (var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(t) })
                {
                    body = client.GetStringAsync(url).GetAwaiter().GetResult();
                }

                string dir = Path.Combine(excelOutputFolder.Trim(), folderSafe);
                Directory.CreateDirectory(dir);

                string fileName = folderSafe + ".txt";
                string path = Path.Combine(dir, fileName);
                File.WriteAllText(path, body, Encoding.UTF8);
                log?.Invoke($"[Лог моста] Сохранён: {path}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Лог моста] Не удалось скачать или сохранить ({url}): {ex.Message}");
            }
        }

        private static string SanitizeForFileName(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(s.Length);
            foreach (char c in s.Trim())
            {
                if (invalid.Contains(c))
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            string r = sb.ToString().Trim();
            return string.IsNullOrEmpty(r) ? "" : r;
        }
    }
}
