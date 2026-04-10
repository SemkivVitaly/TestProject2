using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveData1.Helpers
{
    /// <summary>Результат скачивания логов с моста (для записи в БД и файлы на диск).</summary>
    public sealed class BridgeDownloadResult
    {
        public string UnifiedText { get; set; }
        public string StatusJson { get; set; }
        public string MavlinkJson { get; set; }
    }

    /// <summary>Архивация логов Bridge в папку акта (как в BrigeLogCopy).</summary>
    public static class BridgeLogArchiveService
    {
        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Без_имени";
            var inv = Path.GetInvalidFileNameChars();
            var chars = name.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(inv, chars[i]) >= 0)
                    chars[i] = '_';
            }
            return new string(chars);
        }

        public static async Task<BridgeDownloadResult> SaveLogsFromBridgeAsync(string bridgeBaseUrl, string actFolderPath, string serialRaw)
        {
            string folderName = SanitizeFolderName(serialRaw);
            string targetDir = Path.Combine(actFolderPath.Trim(), folderName);
            Directory.CreateDirectory(targetDir);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            string unified = await BridgeLogClient.DownloadUnifiedLogAsync(bridgeBaseUrl).ConfigureAwait(false);
            File.WriteAllText(Path.Combine(targetDir, "Единый_лог_" + stamp + ".txt"), unified, enc);

            string statusJson = await BridgeLogClient.DownloadStatusJsonAsync(bridgeBaseUrl).ConfigureAwait(false);
            File.WriteAllText(Path.Combine(targetDir, "status_" + stamp + ".json"), statusJson, enc);

            string mavlinkJson = await BridgeLogClient.DownloadMavlinkLogJsonAsync(bridgeBaseUrl).ConfigureAwait(false);
            File.WriteAllText(Path.Combine(targetDir, "mavlink_log_" + stamp + ".json"), mavlinkJson, enc);

            return new BridgeDownloadResult
            {
                UnifiedText = unified,
                StatusJson = statusJson,
                MavlinkJson = mavlinkJson
            };
        }

        public static bool SerialFolderHasSavedContent(string actFolderPath, string serialRaw)
        {
            string path = Path.Combine(actFolderPath.Trim(), SanitizeFolderName(serialRaw));
            if (!Directory.Exists(path))
                return false;
            return Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
