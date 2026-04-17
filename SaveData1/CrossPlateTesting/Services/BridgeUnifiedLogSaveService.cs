using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Сохранение снапшота диагностики ESP32-моста (AsyncWebServer, v2.1+) в папку серийного номера.
    ///
    /// Для каждого серийного номера создаётся подпапка <c>{excelOutputFolder}/{серийный}/</c>,
    /// куда кладутся файлы со штампом времени <c>_yyyyMMdd_HHmmss</c>:
    /// <list type="bullet">
    ///   <item>Единый_лог_*.txt      — /api/log/file       (MAVLink + ESP + статистика)</item>
    ///   <item>esp32_log_*.txt       — /api/log/esp32      (полный ESP-лог)</item>
    ///   <item>status_*.json         — /api/status         (полный снапшот метрик)</item>
    ///   <item>link_*.json           — /api/link           (MAVLink-канал: rx/tx/drops/hb)</item>
    ///   <item>clients_*.json        — /api/clients        (per-slot TCP, UDP-клиент)</item>
    ///   <item>system_stats_*.json   — /api/system/stats   (UART bytes, RSSI, chip_temp)</item>
    ///   <item>mavlink_log_*.json    — /api/log            (кольцевой журнал MAVLink строк)</item>
    /// </list>
    ///
    /// Принцип отказоустойчивости: каждый эндпоинт качается независимо; если один упал —
    /// остальные всё равно сохраняются. Итог и список ошибок пишется в <c>log</c>-callback.
    /// </summary>
    public static class BridgeUnifiedLogSaveService
    {
        /// <summary>Результат снятия снапшота: сколько эндпоинтов удалось сохранить и ошибки.</summary>
        public class SnapshotResult
        {
            public int SavedCount;
            public int TotalCount;
            public List<string> Errors = new List<string>();
            public bool AnySaved => SavedCount > 0;
            public bool AllSaved => SavedCount == TotalCount && TotalCount > 0;
        }

        /// <summary>
        /// Синхронно скачивает все эндпоинты моста после прохождения стендом теста.
        /// Вызывающий код (обычно <c>async void</c>-обработчики UI) гарантированно не блокируется
        /// через синхронизационный контекст — работа выполняется на ThreadPool через <see cref="Task.Run"/>.
        /// </summary>
        /// <param name="excelOutputFolder">Базовый каталог отчётов; внутри создаётся подпапка по серийному номеру.</param>
        /// <param name="stand">Стенд; нужен непустой <see cref="Stand.ProductSerialNumber"/> или <see cref="Stand.Name"/>.</param>
        /// <param name="bridgeHost">Хост HTTP API моста (по умолчанию 192.168.2.1).</param>
        /// <param name="bridgePort">Порт (80 без суффикса в URL).</param>
        /// <param name="timeoutMs">Таймаут HTTP GET на один эндпоинт; фактически не ниже 3000 мс.</param>
        /// <param name="log">Колбэк для сообщений в UI; может быть null.</param>
        public static void TrySaveAfterStandTest(
            string excelOutputFolder,
            Stand stand,
            string bridgeHost,
            int bridgePort,
            int timeoutMs,
            Action<string> log)
        {
            try
            {
                // Task.Run ломает контекст синхронизации, поэтому .GetAwaiter().GetResult() не вызовет deadlock,
                // даже если метод вызван из WinForms UI-потока (как сейчас в CrossPlateTestingPanel).
                Task.Run(() => TrySaveAfterStandTestAsync(
                    excelOutputFolder, stand, bridgeHost, bridgePort, timeoutMs, log, CancellationToken.None))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Лог моста] Внутренняя ошибка архиватора: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Асинхронная версия: скачивает все эндпоинты и сохраняет их в папку серийного номера.
        /// Возвращает <see cref="SnapshotResult"/> с детальным результатом.
        /// </summary>
        public static async Task<SnapshotResult> TrySaveAfterStandTestAsync(
            string excelOutputFolder,
            Stand stand,
            string bridgeHost,
            int bridgePort,
            int timeoutMs,
            Action<string> log,
            CancellationToken ct = default)
        {
            var result = new SnapshotResult();

            if (string.IsNullOrWhiteSpace(excelOutputFolder) || stand == null)
                return result;

            string serial = (stand.ProductSerialNumber ?? stand.Name ?? "").Trim();
            if (string.IsNullOrEmpty(serial))
            {
                log?.Invoke("[Лог моста] Пропуск: не задан серийный номер.");
                return result;
            }

            string folderSafe = SanitizeFolderName(serial);

            string targetDir;
            try
            {
                targetDir = Path.Combine(excelOutputFolder.Trim(), folderSafe);
                Directory.CreateDirectory(targetDir);
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Лог моста] Не удалось создать папку '{folderSafe}': {ex.Message}");
                return result;
            }

            string baseUrl = BridgeLogClient.BuildBaseUrl(bridgeHost, bridgePort);
            int effectiveTimeout = Math.Max(3000, timeoutMs);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            // UTF-8 без BOM — чтобы текстовые логи открывались корректно в любых редакторах.
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            // (Имя файла, лейбл эндпоинта для сообщения об ошибке, загрузчик).
            // Порядок: сперва самые важные (сводный лог), потом детализация по источникам.
            var endpoints = new (string FileName, string Label, Func<Task<string>> Load)[]
            {
                ("Единый_лог_"   + stamp + ".txt",  "/api/log/file",     () => BridgeLogClient.DownloadUnifiedLogAsync(baseUrl, effectiveTimeout, ct)),
                ("esp32_log_"    + stamp + ".txt",  "/api/log/esp32",    () => BridgeLogClient.DownloadEspLogAsync(baseUrl, effectiveTimeout, ct)),
                ("status_"       + stamp + ".json", "/api/status",       () => BridgeLogClient.DownloadStatusJsonAsync(baseUrl, effectiveTimeout, ct)),
                ("link_"         + stamp + ".json", "/api/link",         () => BridgeLogClient.DownloadLinkJsonAsync(baseUrl, effectiveTimeout, ct)),
                ("clients_"      + stamp + ".json", "/api/clients",      () => BridgeLogClient.DownloadClientsJsonAsync(baseUrl, effectiveTimeout, ct)),
                ("system_stats_" + stamp + ".json", "/api/system/stats", () => BridgeLogClient.DownloadSystemStatsJsonAsync(baseUrl, effectiveTimeout, ct)),
                ("mavlink_log_"  + stamp + ".json", "/api/log",          () => BridgeLogClient.DownloadMavlinkLogJsonAsync(baseUrl, effectiveTimeout, ct)),
            };
            result.TotalCount = endpoints.Length;

            foreach (var (fileName, label, load) in endpoints)
            {
                if (ct.IsCancellationRequested)
                {
                    result.Errors.Add("Операция отменена пользователем.");
                    break;
                }

                try
                {
                    string content = await load().ConfigureAwait(false);
                    string fullPath = Path.Combine(targetDir, fileName);
                    // Atomic write: сначала .tmp, потом Move, чтобы при падении не остался полу-записанный файл.
                    string tmpPath = fullPath + ".tmp";
                    File.WriteAllText(tmpPath, content ?? "", utf8);
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                    File.Move(tmpPath, fullPath);
                    result.SavedCount++;
                    log?.Invoke($"[Лог моста] {label} → {fileName}");
                }
                catch (OperationCanceledException)
                {
                    result.Errors.Add(label + ": таймаут/отмена");
                    log?.Invoke($"[Лог моста] {label}: таймаут {effectiveTimeout} мс");
                }
                catch (Exception ex)
                {
                    result.Errors.Add(label + ": " + ex.Message);
                    log?.Invoke($"[Лог моста] {label}: ошибка — {ex.GetType().Name}: {ex.Message}");
                }
            }

            // Итог одной строкой — удобно читать в консоли.
            if (result.AllSaved)
                log?.Invoke($"[Лог моста] Снапшот полностью сохранён в '{targetDir}' ({result.SavedCount}/{result.TotalCount}).");
            else if (result.AnySaved)
                log?.Invoke($"[Лог моста] Снапшот сохранён частично в '{targetDir}' ({result.SavedCount}/{result.TotalCount}).");
            else
                log?.Invoke($"[Лог моста] Снапшот НЕ сохранён ({baseUrl}). Проверьте Wi-Fi и доступность моста.");

            return result;
        }

        /// <summary>
        /// Уже существуют ли сохранённые файлы логов для указанного серийного номера.
        /// Полезно для предупреждения о перезаписи/дублировании.
        /// </summary>
        public static bool SerialFolderHasSavedContent(string excelOutputFolder, string serialRaw)
        {
            if (string.IsNullOrWhiteSpace(excelOutputFolder) || string.IsNullOrWhiteSpace(serialRaw))
                return false;
            string path = Path.Combine(excelOutputFolder.Trim(), SanitizeFolderName(serialRaw));
            if (!Directory.Exists(path))
                return false;
            try { return Directory.EnumerateFileSystemEntries(path).Any(); }
            catch { return false; }
        }

        /// <summary>Безопасное имя папки для № акта или серийного номера.</summary>
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
            string result = new string(chars).Trim();
            return string.IsNullOrEmpty(result) ? "Без_имени" : result;
        }
    }
}
