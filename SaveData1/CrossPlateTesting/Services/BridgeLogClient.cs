using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// HTTP-клиент для ESP32-моста DroneBridge (AsyncWebServer, v2.1+).
    ///
    /// Поддерживаемые эндпоинты прошивки:
    /// <list type="bullet">
    ///   <item><c>GET /api/log/file</c> — сводный лог (text/plain utf-8): MAVLink + ESP + статистика.</item>
    ///   <item><c>GET /api/log</c> — кольцевой журнал MAVLink (JSON-массив строк).</item>
    ///   <item><c>GET /api/log/esp32</c> — лог ESP32 (text/plain utf-8).</item>
    ///   <item><c>GET /api/status</c> — полный снапшот метрик (uptime, heap, chip_temp, MAVLink-счётчики,
    ///       uart_bytes_*, net_bytes_*, rssi_*, счётчики подключений, SERVO-параметры).</item>
    ///   <item><c>GET /api/link</c> — компактный JSON канала MAVLink (packets_*, drops, loss_pct, heartbeat_age_ms).</item>
    ///   <item><c>GET /api/clients</c> — per-slot статистика TCP и текущий UDP-клиент.</item>
    ///   <item><c>GET /api/system/stats</c> — UART bytes, RSSI, tcp/udp клиенты, chip_temp (совместимо со старым UI).</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Используется один статический <see cref="HttpClient"/> (рекомендуется MS для .NET Framework).
    /// Таймаут задаётся на уровне запроса через <see cref="CancellationToken"/>, поэтому общий timeout
    /// клиента выставлен большим с запасом.
    /// </remarks>
    public static class BridgeLogClient
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            // Общий таймаут большой; реальный per-request контроль — через CancellationToken.
            var c = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            c.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
            return c;
        }

        /// <summary>
        /// Нормализует базовый URL. Поддерживает ввод вида "192.168.2.1", "http://192.168.2.1",
        /// "192.168.2.1:8080". Добавляет схему http:// если отсутствует.
        /// </summary>
        public static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "http://192.168.2.1";
            var u = baseUrl.Trim().TrimEnd('/');
            if (!u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                u = "http://" + u;
            return u;
        }

        /// <summary>
        /// Собирает базовый URL из host+port. Порт 80 и 443 не включаются в URL (оставляется схема по умолчанию).
        /// </summary>
        public static string BuildBaseUrl(string host, int port)
        {
            string h = string.IsNullOrWhiteSpace(host) ? "192.168.2.1" : host.Trim();
            if (port <= 0 || port == 80)
                return "http://" + h;
            if (port == 443)
                return "https://" + h;
            return "http://" + h + ":" + port.ToString();
        }

        /* ===== Текстовые (utf-8) ===== */

        public static Task<string> DownloadUnifiedLogAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/log/file", timeoutMs, ct);

        public static Task<string> DownloadEspLogAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/log/esp32", timeoutMs, ct);

        /* ===== JSON ===== */

        public static Task<string> DownloadStatusJsonAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/status", timeoutMs, ct);

        public static Task<string> DownloadMavlinkLogJsonAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/log", timeoutMs, ct);

        public static Task<string> DownloadLinkJsonAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/link", timeoutMs, ct);

        public static Task<string> DownloadClientsJsonAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/clients", timeoutMs, ct);

        public static Task<string> DownloadSystemStatsJsonAsync(string baseUrl, int timeoutMs = 15000, CancellationToken ct = default) =>
            DownloadTextAsync(baseUrl, "/api/system/stats", timeoutMs, ct);

        /// <summary>
        /// Читает тело ответа как UTF-8 (независимо от Content-Type) и возвращает строку.
        /// Для text/plain это сам лог, для application/json — JSON-текст.
        /// </summary>
        /// <param name="timeoutMs">Таймаут на запрос; если <=0, применяется 15 000 мс.</param>
        /// <param name="ct">Внешний токен отмены; объединяется с внутренним таймером.</param>
        private static async Task<string> DownloadTextAsync(string baseUrl, string path, int timeoutMs, CancellationToken ct)
        {
            var url = NormalizeBaseUrl(baseUrl) + path;
            int effectiveTimeout = timeoutMs > 0 ? timeoutMs : 15000;

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                linked.CancelAfter(effectiveTimeout);
                var bytes = await Http.GetByteArrayAsync(url).WithCancellation(linked.Token).ConfigureAwait(false);
                return Encoding.UTF8.GetString(bytes);
            }
        }

        /// <summary>
        /// Обёртка для привязки внешнего <see cref="CancellationToken"/> к Task, у которого нативной отмены нет.
        /// При отмене бросает <see cref="OperationCanceledException"/>; исходный Task продолжит работу в фоне
        /// (у <see cref="HttpClient.GetByteArrayAsync(string)"/> нет перегрузки с CancellationToken до .NET 5).
        /// </summary>
        private static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled) return await task.ConfigureAwait(false);
            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                var winner = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (winner != task)
                    throw new OperationCanceledException(ct);
                return await task.ConfigureAwait(false);
            }
        }
    }
}
