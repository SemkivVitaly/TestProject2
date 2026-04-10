using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SaveData1.Helpers
{
    /// <summary>Подключение к OBS по WebSocket, запуск и остановка записи.</summary>
    public sealed class ObsWebSocketHelper : IDisposable
    {
        private ClientWebSocket _ws;
        private readonly int _timeoutMs;

        public ObsWebSocketHelper(int timeoutMs = 5000)
        {
            _timeoutMs = timeoutMs;
        }

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        /// <summary>Подключение к OBS WebSocket и аутентификация.</summary>
        public bool Connect(string ip, int port, string password)
        {
            try
            {
                return Task.Run(() => ConnectAsync(ip, port, password)).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Запуск записи в OBS.</summary>
        public bool StartRecording()
        {
            try
            {
                return Task.Run(() => SendRequest("StartRecord", "startRec")).GetAwaiter().GetResult();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Остановка записи и возврат пути к записанному файлу.</summary>
        public string StopRecording()
        {
            try
            {
                return Task.Run(() => StopRecordAsync()).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Закрытие WebSocket-соединения.</summary>
        public void Disconnect()
        {
            try
            {
                if (_ws != null && _ws.State == WebSocketState.Open)
                    Task.Run(() => _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None))
                        .Wait(_timeoutMs);
            }
            catch { }
            finally
            {
                _ws?.Dispose();
                _ws = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        #region Async internals

        private async Task<bool> ConnectAsync(string ip, int port, string password)
        {
            _ws = new ClientWebSocket();
            var cts = new CancellationTokenSource(_timeoutMs);
            await _ws.ConnectAsync(new Uri($"ws://{ip}:{port}"), cts.Token);

            string hello = await ReceiveMessage(cts.Token);
            int? op = JsonGetInt(hello, "op");
            if (op != 0) return false;

            string challenge = JsonGetString(hello, "challenge");
            string salt = JsonGetString(hello, "salt");

            string identifyMsg;
            if (challenge != null && salt != null && !string.IsNullOrEmpty(password))
            {
                string auth = ComputeAuth(password, salt, challenge);
                identifyMsg = "{\"op\":1,\"d\":{\"rpcVersion\":1,\"authentication\":\"" + auth + "\",\"eventSubscriptions\":0}}";
            }
            else
            {
                identifyMsg = "{\"op\":1,\"d\":{\"rpcVersion\":1,\"eventSubscriptions\":0}}";
            }

            await SendMessage(identifyMsg, cts.Token);
            string identified = await ReceiveMessage(cts.Token);
            return JsonGetInt(identified, "op") == 2;
        }

        private async Task<string> StopRecordAsync()
        {
            if (!IsConnected) return null;
            var cts = new CancellationTokenSource(_timeoutMs);
            string msg = "{\"op\":6,\"d\":{\"requestType\":\"StopRecord\",\"requestId\":\"stopRec\"}}";
            await SendMessage(msg, cts.Token);
            string resp = await ReceiveMessage(cts.Token);
            if (resp == null || !resp.Contains("\"requestStatus\"")) return null;
            string outputPath = JsonGetString(resp, "outputPath");
            if (outputPath != null)
                outputPath = outputPath.Replace("\\\\", "\\");
            return outputPath;
        }

        private async Task<bool> SendRequest(string requestType, string requestId)
        {
            if (!IsConnected) return false;
            var cts = new CancellationTokenSource(_timeoutMs);
            string msg = "{\"op\":6,\"d\":{\"requestType\":\"" + requestType + "\",\"requestId\":\"" + requestId + "\"}}";
            await SendMessage(msg, cts.Token);
            string resp = await ReceiveMessage(cts.Token);
            return resp != null && resp.Contains("\"requestStatus\"");
        }

        private async Task SendMessage(string msg, CancellationToken ct)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        private async Task<string> ReceiveMessage(CancellationToken ct)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);
            return sb.ToString();
        }

        #endregion

        #region Auth helpers

        private static string ComputeAuth(string password, string salt, string challenge)
        {
            using (var sha = SHA256.Create())
            {
                byte[] passAndSalt = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
                string base64Secret = Convert.ToBase64String(passAndSalt);
                byte[] secretAndChallenge = sha.ComputeHash(Encoding.UTF8.GetBytes(base64Secret + challenge));
                return Convert.ToBase64String(secretAndChallenge);
            }
        }

        #endregion

        #region Simple JSON value extraction

        private static string JsonGetString(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static int? JsonGetInt(string json, string key)
        {
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(\\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : (int?)null;
        }

        #endregion
    }
}
