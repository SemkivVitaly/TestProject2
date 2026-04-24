using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Выполнение последовательности: Wi-Fi → Mission Planner → скрипт
    /// </summary>
    public class StandRunnerService
    {
        private readonly Action<string> _log;
        private CancellationTokenSource _cts;

        public bool IsRunning { get; private set; }
        public event Action OnCompleted;
        public event Action OnStopped;

        public StandRunnerService(Action<string> logAction)
        {
            _log = logAction ?? (s => { });
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Запуск теста для одного стенда: Wi‑Fi → Mission Planner (если указан) → скрипт.
        /// </summary>
        /// <returns>true, если тест выполнен успешно.</returns>
        public async Task<bool> RunSingleStandAsync(AppConfig config, Stand stand)
        {
            if (IsRunning)
            {
                _log("Выполняется другой процесс. Дождитесь завершения.");
                return false;
            }

            if (!stand.HasSavedCredentials || string.IsNullOrWhiteSpace(stand.WifiSsid))
            {
                _log($"[ОШИБКА] Стенд '{stand.Name}' — нет сохранённых данных Wi-Fi.");
                return false;
            }

            bool hasScript = !string.IsNullOrWhiteSpace(config.ScriptPath) && File.Exists(config.ScriptPath);
            bool hasMavParamsScript = hasScript && MavParamScriptService.IsMavParamScript(config.ScriptPath);
            bool hasExecutableScript = hasScript && !hasMavParamsScript;
            if (!hasScript)
            {
                _log("Укажите путь к скрипту (.mavparams, .bat, .ps1).");
                return false;
            }

            IsRunning = true;
            _cts = new CancellationTokenSource();
            bool success = false;

            try
            {
                _log($"[{DateTime.Now:HH:mm:ss}] === ТЕСТ СТЕНДА: {stand.Name} ===");

                if (!await ConnectToWifiAsync(stand))
                {
                    _log($"[ОШИБКА] Не удалось подключиться к Wi-Fi '{stand.WifiSsid}'.");
                    return false;
                }

                _log("[Тест] Ожидание стабилизации сети...");
                await Task.Delay(DelaySettings.Stand_NetworkStabilityTest, _cts.Token);

                if (!string.IsNullOrWhiteSpace(config.MissionPlannerPath) && File.Exists(config.MissionPlannerPath))
                {
                    if (!IsMissionPlannerRunning(config))
                    {
                        _log("[Mission Planner] Запуск Mission Planner...");
                        StartMissionPlannerWithConnection(config);
                        await Task.Delay(DelaySettings.Stand_MissionPlannerStart, _cts.Token);
                    }
                    else
                    {
                        _log("[Mission Planner] Mission Planner уже запущен.");
                    }
                    if (!await WaitForDroneConnectionAsync(config))
                    {
                        _log($"[ОШИБКА] Подключение к дрону не установлено. Скрипт не выполняется.");
                        return false;
                    }
                }
                else
                {
                    _log("[ИНФО] Mission Planner не указан. Ожидание для стабилизации сети...");
                    await Task.Delay(DelaySettings.Stand_NetworkStabilityNoMp, _cts.Token);
                    if (!await WaitForDroneConnectionAsync(config))
                    {
                        _log($"[ОШИБКА] Подключение к дрону не установлено. Скрипт не выполняется.");
                        return false;
                    }
                }

                if (hasMavParamsScript)
                {
                    _log($"[MAVLink] Выполнение скрипта для '{stand.Name}'...");
                    success = await RunMavParamScriptAsync(config);
                    if (success)
                        _log($"[OK] Тест '{stand.Name}' завершён успешно.");
                    else
                        _log($"[ОШИБКА] Тест '{stand.Name}' завершён с ошибками.");
                }
                else if (hasExecutableScript)
                {
                    success = await RunScriptAsync(config);
                    if (success)
                        _log($"[OK] Тест '{stand.Name}' завершён успешно.");
                    else
                        _log($"[ОШИБКА] Ошибка запуска скрипта для '{stand.Name}'.");
                }

                _log($"[{DateTime.Now:HH:mm:ss}] === КОНЕЦ ТЕСТА: {stand.Name} ===");
                OnCompleted?.Invoke();
                return success;
            }
            catch (OperationCanceledException)
            {
                _log("[ОСТАНОВЛЕНО] Тест прерван.");
                OnStopped?.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                _log($"[ОШИБКА] {ex.Message}");
                OnCompleted?.Invoke();
                return false;
            }
            finally
            {
                IsRunning = false;
            }
        }

        public async Task RunAsync(AppConfig config, Action<Stand, bool> onTestComplete = null)
        {
            if (IsRunning)
            {
                _log("Уже выполняется. Дождитесь завершения.");
                // OnCompleted НЕ вызываем — предыдущий запуск сам его вызовет.
                return;
            }

            if (config.Stands == null || config.Stands.Count == 0)
            {
                _log("Нет стендов для обработки. Добавьте стенды и сохраните данные.");
                // Обязательно возвращаем UI в исходное состояние, чтобы кнопки «Старт/Стоп»
                // не оставались заблокированными (форма «зависает»).
                OnCompleted?.Invoke();
                return;
            }

            bool hasScript = !string.IsNullOrWhiteSpace(config.ScriptPath) && File.Exists(config.ScriptPath);
            bool hasMavParamsScript = hasScript && MavParamScriptService.IsMavParamScript(config.ScriptPath);
            bool hasExecutableScript = hasScript && !hasMavParamsScript;
            if (!hasScript)
            {
                _log("Укажите путь к скрипту (.mavparams, .bat, .ps1).");
                // Без скрипта запускать нечего — разблокируем UI, иначе пользователь
                // не сможет нажать «Старт» повторно после указания скрипта.
                OnCompleted?.Invoke();
                return;
            }

            IsRunning = true;
            _cts = new CancellationTokenSource();

            try
            {
                _log($"[{DateTime.Now:HH:mm:ss}] === СТАРТ ОБРАБОТКИ {config.Stands.Count} СТЕНДОВ ===");

                bool missionPlannerStarted = false;

                foreach (var stand in config.Stands)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        _log("[ОСТАНОВЛЕНО] Пользователь остановил выполнение.");
                        break;
                    }

                    if (!stand.HasSavedCredentials || string.IsNullOrWhiteSpace(stand.WifiSsid))
                    {
                        _log($"[ПРОПУСК] Серийный номер '{stand.Name}' - нет сохранённых данных Wi-Fi.");
                        continue;
                    }

                    _log($"[{DateTime.Now:HH:mm:ss}] --- Серийный номер: {stand.Name} ---");

                    // 1. Подключение к Wi-Fi
                    if (!await ConnectToWifiAsync(stand))
                    {
                        _log($"[ОШИБКА] Не удалось подключиться к Wi-Fi '{stand.WifiSsid}'. Пропуск.");
                        continue;
                    }

                    // 2. Mission Planner и скрипт
                    if (!string.IsNullOrWhiteSpace(config.MissionPlannerPath) && File.Exists(config.MissionPlannerPath))
                    {
                        bool isFirstStand = !missionPlannerStarted;

                        // Первый стенд: если Mission Planner не запущен — открыть его
                        if (isFirstStand && !IsMissionPlannerRunning(config))
                        {
                            _log("[Mission Planner] Запуск Mission Planner...");
                            StartMissionPlannerWithConnection(config);
                            missionPlannerStarted = true;
                            await Task.Delay(DelaySettings.Stand_MissionPlannerStart, _cts.Token);
                        }
                        else if (isFirstStand)
                        {
                            missionPlannerStarted = true;
                            _log("[Mission Planner] Mission Planner уже запущен.");
                        }
                        else
                        {
                            _log("[Mission Planner] Переход к следующему стенду. Mission Planner уже открыт, повторный запуск не требуется.");
                        }

                        // Проверка подключения к дрону по MAVLink (heartbeat)
                        if (!await WaitForDroneConnectionAsync(config))
                        {
                            _log($"[ОШИБКА] Подключение к дрону не установлено. Скрипт не выполняется.");
                            onTestComplete?.Invoke(stand, false);
                            continue;
                        }

                        // Скрипт параметров .mavparams — несколько параметров в одном файле
                        bool standSuccess = true;
                        if (hasMavParamsScript)
                        {
                            standSuccess = await RunMavParamScriptAsync(config);
                        }

                        // Выполнение обычного скрипта (.bat, .ps1)
                        if (hasExecutableScript)
                        {
                            if (!await RunScriptAsync(config))
                            {
                                _log($"[ОШИБКА] Ошибка запуска скрипта для серийного номера '{stand.Name}'.");
                                standSuccess = false;
                            }
                            else
                                _log($"[Mission Planner] Скрипт выполнен: {Path.GetFileName(config.ScriptPath)}");
                        }
                        onTestComplete?.Invoke(stand, standSuccess);
                    }
                    else
                    {
                        _log("[ИНФО] Mission Planner не указан. Ожидание для стабилизации сети...");
                        await Task.Delay(DelaySettings.Stand_NetworkStabilityNoMp, _cts.Token);
                        if (!await WaitForDroneConnectionAsync(config))
                        {
                            _log($"[ОШИБКА] Подключение к дрону не установлено. Скрипт не выполняется.");
                            onTestComplete?.Invoke(stand, false);
                            continue;
                        }
                        bool standSuccess = true;
                        if (hasMavParamsScript)
                        {
                            standSuccess = await RunMavParamScriptAsync(config);
                        }
                        if (hasExecutableScript)
                        {
                            if (!await RunScriptAsync(config))
                            {
                                _log($"[ОШИБКА] Ошибка запуска скрипта для серийного номера '{stand.Name}'.");
                                standSuccess = false;
                            }
                        }
                        onTestComplete?.Invoke(stand, standSuccess);
                    }

                    // Пауза перед следующим стендом
                    if (!_cts.Token.IsCancellationRequested)
                        await Task.Delay(DelaySettings.Stand_PauseBetweenStands, _cts.Token);
                }

                _log($"[{DateTime.Now:HH:mm:ss}] === ЗАВЕРШЕНО. Ожидание следующего запуска. ===");
                OnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                _log("[ОСТАНОВЛЕНО] Выполнение прервано.");
                OnStopped?.Invoke();
            }
            catch (Exception ex)
            {
                _log($"[ОШИБКА] {ex.Message}");
                OnCompleted?.Invoke();
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task<bool> ConnectToWifiAsync(Stand stand)
        {
            try
            {
                _log($"[Wi-Fi] Подключение к сети '{stand.WifiSsid}'...");

                // 1) Если уже подключены к той же сети — ничего не делаем.
                if (IsConnectedToWifi(stand.WifiSsid))
                {
                    _log($"[Wi-Fi] Уже подключены к '{stand.WifiSsid}'. Шаг подключения пропущен.");
                    return true;
                }

                // 2) Добавляем WLAN-профиль (идемпотентно).
                string profileXml = CreateWifiProfileXml(stand.WifiSsid, stand.WifiPassword);
                string tempPath = Path.Combine(Path.GetTempPath(), $"wifi_{stand.Id}.xml");
                try { File.WriteAllText(tempPath, profileXml); }
                catch (Exception ex)
                {
                    _log($"[Wi-Fi] Не удалось записать временный профиль ({tempPath}): {ex.Message}");
                    return false;
                }

                (int addCode, string addOut, string addErr) =
                    await RunNetshAsync($"wlan add profile filename=\"{tempPath}\" user=all", 10000).ConfigureAwait(false);
                if (File.Exists(tempPath))
                    try { File.Delete(tempPath); } catch { }
                if (addCode != 0 && !string.IsNullOrWhiteSpace(addErr))
                    _log($"[Wi-Fi] 'add profile' код={addCode}: {addErr.Trim()}");

                // 3) Форсированно просим повторное сканирование — иначе netsh может не видеть свежую точку.
                try { await RunNetshAsync("wlan show networks mode=bssid", 5000).ConfigureAwait(false); } catch { }

                // 4) Основной цикл подключения с ретраями — адаптер может «зависать» на первой попытке.
                int connectAttempts = 3;
                for (int attempt = 1; attempt <= connectAttempts; attempt++)
                {
                    if (_cts?.Token.IsCancellationRequested == true) return false;

                    (int code, string outp, string err) =
                        await RunNetshAsync($"wlan connect name=\"{stand.WifiSsid}\"", 15000).ConfigureAwait(false);
                    string combined = ((outp ?? "") + " " + (err ?? "")).Trim();
                    if (!string.IsNullOrEmpty(combined))
                        _log($"[Wi-Fi] netsh: {combined}");

                    // Дожидаемся ассоциации.
                    int totalWaitMs = Math.Max(2000, DelaySettings.Stand_WifiAfterConnect)
                                    + Math.Max(2000, DelaySettings.Stand_WifiRetryCheck);
                    int stepMs = 500;
                    int waited = 0;
                    while (waited < totalWaitMs)
                    {
                        if (_cts?.Token.IsCancellationRequested == true) return false;
                        try { await Task.Delay(stepMs, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false); }
                        catch (OperationCanceledException) { return false; }
                        waited += stepMs;

                        if (IsConnectedToWifi(stand.WifiSsid))
                        {
                            _log($"[Wi-Fi] Успешно подключено к '{stand.WifiSsid}' (попытка {attempt}).");
                            return true;
                        }
                    }

                    _log($"[Wi-Fi] Попытка {attempt}/{connectAttempts} не удалась. Текущий SSID: '{WifiInfoService.GetCurrentSsid() ?? "—"}'.");

                    // Перед новой попыткой — короткая пауза и (на 2-й попытке) отключение/перезапуск адаптера.
                    if (attempt < connectAttempts)
                    {
                        try { await Task.Delay(1500, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false); }
                        catch (OperationCanceledException) { return false; }

                        if (attempt == connectAttempts - 1)
                        {
                            // Мягкий «сброс»: disconnect, подождать, потом попытка снова.
                            try { await RunNetshAsync("wlan disconnect", 5000).ConfigureAwait(false); } catch { }
                            try { await Task.Delay(1000, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false); }
                            catch (OperationCanceledException) { return false; }
                        }
                    }
                }

                // 5) Не подключились — даём пользователю осмысленный совет.
                var availableNetworks = WifiInfoService.GetAvailableNetworks();
                bool seeNetwork = availableNetworks != null &&
                    availableNetworks.Any(s => string.Equals(s, stand.WifiSsid, StringComparison.OrdinalIgnoreCase));
                if (!seeNetwork)
                {
                    _log($"[Wi-Fi] Сеть '{stand.WifiSsid}' не видна адаптером. Проверьте: стенд включён, антенна близко, Wi‑Fi адаптер не отключён.");
                }
                else
                {
                    _log($"[Wi-Fi] Сеть '{stand.WifiSsid}' видна, но подключиться не удалось. Возможно неверный пароль или конфликт профиля — пересохраните Wi‑Fi-данные стенда.");
                }
                return false;
            }
            catch (Exception ex)
            {
                _log($"[Wi-Fi] Ошибка: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>Запуск netsh с захватом stdout/stderr и кода возврата.</summary>
        private static Task<(int exitCode, string output, string error)> RunNetshAsync(string arguments, int timeoutMs)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                try
                {
                    using (var p = Process.Start(psi))
                    {
                        string o = p?.StandardOutput.ReadToEnd() ?? "";
                        string e = p?.StandardError.ReadToEnd() ?? "";
                        p?.WaitForExit(Math.Max(1000, timeoutMs));
                        int code = p != null && p.HasExited ? p.ExitCode : -1;
                        return (code, o, e);
                    }
                }
                catch (Exception ex)
                {
                    return (-1, "", ex.Message);
                }
            });
        }

        private bool IsConnectedToWifi(string ssid)
        {
            if (string.IsNullOrWhiteSpace(ssid)) return false;
            try
            {
                string current = WifiInfoService.GetCurrentSsid();
                if (string.IsNullOrEmpty(current)) return false;
                return string.Equals(current, ssid, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string CreateWifiProfileXml(string ssid, string password)
        {
            string escapedSsid = System.Security.SecurityElement.Escape(ssid);
            string escapedPwd = System.Security.SecurityElement.Escape(password ?? "");
            return $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{escapedSsid}</name>
  <SSIDConfig>
    <SSID>
      <name>{escapedSsid}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>WPA2PSK</authentication>
        <encryption>AES</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
      <sharedKey>
        <keyType>passPhrase</keyType>
        <protected>false</protected>
        <keyMaterial>{escapedPwd}</keyMaterial>
      </sharedKey>
    </security>
  </MSM>
</WLANProfile>";
        }

        private void StartMissionPlannerWithConnection(AppConfig config)
        {
            try
            {
                string host = "192.168.4.1";
                int port = config.DronePort > 0 ? config.DronePort : 14550;
                if (!string.IsNullOrWhiteSpace(config.DronePingAddress))
                {
                    var addrs = config.DronePingAddress.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (addrs.Length > 0) host = addrs[0].Trim();
                }
                string connectionArg = $"udpout:{host}:{port}";
                _log($"[Mission Planner] Подключение: {connectionArg}");
                var startInfo = new ProcessStartInfo
                {
                    FileName = config.MissionPlannerPath,
                    Arguments = connectionArg,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                _log($"[Mission Planner] Ошибка запуска: {ex.Message}. Запуск без параметров.");
                try { Process.Start(config.MissionPlannerPath); } catch { }
            }
        }

        private bool IsMissionPlannerRunning(AppConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.MissionPlannerPath)) return false;
                string processName = Path.GetFileNameWithoutExtension(config.MissionPlannerPath);
                return Process.GetProcessesByName(processName).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> WaitForDroneConnectionAsync(AppConfig config)
        {
            try
            {
                if (config.SkipMavLinkConnectionCheck)
                {
                    _log("[Проверка] Режим без MAVLink: проверка Ping/UDP (скрипт выполнится в любом случае)...");
                    int timeoutMs = Math.Max(5000, config.ConnectionTimeoutSeconds * 1000);
                    int elapsed = 0;
                    int step = DelaySettings.Stand_ConnectionCheckStep;

                    while (elapsed < timeoutMs && !_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(step, _cts.Token);
                        elapsed += step;

                        if (IsDroneNetworkReachable(config))
                        {
                            _log("[Ping/UDP] Дрон доступен в сети. Выполнение скрипта.");
                            return true;
                        }
                    }

                    _log("[Ping/UDP] Дрон не отвечает на Ping/UDP. Скрипт выполняется без проверки MAVLink.");
                    return true;
                }

                string host = "192.168.4.1";
                int port = config.DronePort > 0 ? config.DronePort : 14550;
                if (!string.IsNullOrWhiteSpace(config.DronePingAddress))
                {
                    var addrs = config.DronePingAddress.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (addrs.Length > 0) host = addrs[0].Trim();
                }

                _log("[MAVLink] Ожидание подключения к дрону (heartbeat)...");
                int mavTimeoutMs = Math.Max(5000, config.ConnectionTimeoutSeconds * 1000);

                bool ok = await MavLinkService.CheckConnectionAsync(host, port, mavTimeoutMs, _cts?.Token ?? CancellationToken.None);
                if (ok)
                    _log("[MAVLink] Подключение к дрону установлено, получен ответ.");
                else
                    _log($"[MAVLink] Дрон не ответил за {mavTimeoutMs / 1000} сек (heartbeat не получен).");
                return ok;
            }
            catch (Exception ex)
            {
                _log($"[Ошибка] Проверка подключения: {ex.Message}");
                return false;
            }
        }

        private bool IsDroneNetworkReachable(AppConfig config)
        {
            try
            {
                string pingAddresses = (config.DronePingAddress ?? "").Trim();
                if (string.IsNullOrEmpty(pingAddresses)) pingAddresses = "192.168.4.1;192.168.1.1";

                int port = config.DronePort > 0 ? config.DronePort : 14550;
                var addresses = pingAddresses.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var addr in addresses)
                {
                    var trimmed = addr.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    if (IsUdpPortReachable(trimmed, port, 500))
                        return true;
                    if (PingHost(trimmed, 500))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsUdpPortReachable(string host, int port, int timeoutMs)
        {
            // Синхронная версия без BeginReceive — чтобы избежать
            // ObjectDisposedException при Dispose(UdpClient) с незавершённой асинхронной операцией.
            UdpClient client = null;
            try
            {
                client = new UdpClient();
                client.Client.ReceiveTimeout = timeoutMs;
                client.Connect(host, port);
                byte[] probe = new byte[] { 0 };
                client.Send(probe, probe.Length);
                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    byte[] data = client.Receive(ref remoteEp);
                    return data != null && data.Length > 0;
                }
                catch (SocketException)
                {
                    // Тайм-аут ожидания — нормально.
                    return false;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                try { client?.Close(); } catch { }
            }
        }

        private bool PingHost(string host, int timeoutMs)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(host, timeoutMs);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RunMavParamScriptAsync(AppConfig config)
        {
            string host = "192.168.4.1";
            int port = config.DronePort > 0 ? config.DronePort : 14550;
            if (!string.IsNullOrWhiteSpace(config.DronePingAddress))
            {
                var addrs = config.DronePingAddress.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (addrs.Length > 0) host = addrs[0].Trim();
            }

            _log($"[MAVLink] Выполнение скрипта: {Path.GetFileName(config.ScriptPath)}");
            bool ok = await ScriptNodeTreeExecutor.RunFromFileAsync(config.ScriptPath, host, port, _log, _cts?.Token ?? CancellationToken.None);
            if (ok)
                _log("[MAVLink] Скрипт параметров выполнен.");
            return ok;
        }

        /// <summary>
        /// Режим мониторинга: сканирование сетей, подключение к стендам с доступной сетью и серийным номером, выполнение теста.
        /// </summary>
        public async Task RunMonitoringAsync(AppConfig config, Dictionary<string, string> lastTestedByStand, Action<Stand, bool> onTestComplete)
        {
            if (IsRunning)
            {
                _log("Уже выполняется. Дождитесь завершения.");
                return;
            }

            if (config.Stands == null || config.Stands.Count == 0)
            {
                _log("Нет стендов для обработки. Добавьте стенды и сохраните данные.");
                // Разблокируем UI: без стендов запускать нечего.
                OnCompleted?.Invoke();
                return;
            }

            bool hasScript = !string.IsNullOrWhiteSpace(config.ScriptPath) && File.Exists(config.ScriptPath);
            bool hasMavParamsScript = hasScript && MavParamScriptService.IsMavParamScript(config.ScriptPath);
            bool hasExecutableScript = hasScript && !hasMavParamsScript;
            if (!hasScript)
            {
                _log("Укажите путь к скрипту (.mavparams, .bat, .ps1).");
                // Без скрипта мониторинг тоже запускать нечего — разблокируем UI.
                OnCompleted?.Invoke();
                return;
            }

            IsRunning = true;
            _cts = new CancellationTokenSource();

            try
            {
                _log($"[{DateTime.Now:HH:mm:ss}] === СТАРТ МОНИТОРИНГА ===");

                while (!_cts.Token.IsCancellationRequested)
                {
                    var standsCopy = config.Stands?.ToList() ?? new List<Stand>();
                    int testsRun = 0;
                    int scanDelay = Math.Max(0, DelaySettings.Stand_NetworkScanDelayMs);
                    for (int i = 0; i < standsCopy.Count; i++)
                    {
                        if (_cts.Token.IsCancellationRequested) break;

                        var stand = standsCopy[i];
                        if (i > 0 && scanDelay > 0 && !_cts.Token.IsCancellationRequested)
                            await Task.Delay(scanDelay, _cts.Token);

                        if (string.IsNullOrWhiteSpace(stand.ProductSerialNumber))
                        {
                            _log($"[Мониторинг] Пропуск '{stand.Name}': не указан серийный номер продукта.");
                            continue;
                        }
                        if (!stand.HasSavedCredentials || string.IsNullOrWhiteSpace(stand.WifiSsid))
                        {
                            _log($"[Мониторинг] Пропуск '{stand.Name}': нет сохранённых данных Wi-Fi.");
                            continue;
                        }
                        if (!WifiInfoService.IsNetworkAvailable(stand.WifiSsid))
                        {
                            _log($"[Мониторинг] Пропуск '{stand.Name}': сеть '{stand.WifiSsid}' недоступна.");
                            continue;
                        }
                        if (lastTestedByStand.TryGetValue(stand.Id, out var last) && last == stand.ProductSerialNumber)
                        {
                            _log($"[Мониторинг] Пропуск '{stand.Name}': SN {stand.ProductSerialNumber} уже протестирован.");
                            continue;
                        }

                        _log($"[{DateTime.Now:HH:mm:ss}] --- Мониторинг: {stand.Name} (SN: {stand.ProductSerialNumber}) ---");

                        if (!await ConnectToWifiAsync(stand))
                        {
                            _log($"[ОШИБКА] Не удалось подключиться к Wi-Fi '{stand.WifiSsid}'. Пропуск.");
                            continue;
                        }

                        _log("[Тест] Ожидание стабилизации сети...");
                        await Task.Delay(DelaySettings.Stand_NetworkStabilityTest, _cts.Token);

                        if (!await WaitForDroneConnectionAsync(config))
                        {
                            _log($"[ОШИБКА] Подключение к дрону не установлено. Скрипт не выполняется.");
                            continue;
                        }

                        bool success = false;
                        if (hasMavParamsScript)
                        {
                            string host = "192.168.4.1";
                            int port = config.DronePort > 0 ? config.DronePort : 14550;
                            if (!string.IsNullOrWhiteSpace(config.DronePingAddress))
                            {
                                var addrs = config.DronePingAddress.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (addrs.Length > 0) host = addrs[0].Trim();
                            }
                            success = await ScriptNodeTreeExecutor.RunFromFileAsync(config.ScriptPath, host, port, _log, _cts?.Token ?? CancellationToken.None);
                        }
                        else if (hasExecutableScript)
                        {
                            success = await RunScriptAsync(config);
                        }

                        if (success)
                        {
                            testsRun++;
                            lastTestedByStand[stand.Id] = stand.ProductSerialNumber;
                            onTestComplete?.Invoke(stand, true);
                            _log($"[OK] Тест '{stand.Name}' завершён успешно.");
                        }
                        else
                        {
                            _log($"[ОШИБКА] Тест '{stand.Name}' завершён с ошибками.");
                        }

                        if (!_cts.Token.IsCancellationRequested)
                            await Task.Delay(DelaySettings.Stand_PauseBetweenStands, _cts.Token);
                    }

                    int interval = Math.Max(5000, DelaySettings.Stand_MonitoringScanIntervalMs);
                    _log($"[Мониторинг] Проверено {standsCopy.Count} стендов, выполнено {testsRun} тестов. Пауза {interval / 1000} сек...");
                    await Task.Delay(interval, _cts.Token);
                }

                _log($"[{DateTime.Now:HH:mm:ss}] === МОНИТОРИНГ ОСТАНОВЛЕН ===");
                OnStopped?.Invoke();
            }
            catch (OperationCanceledException)
            {
                _log("[ОСТАНОВЛЕНО] Мониторинг прерван.");
                OnStopped?.Invoke();
            }
            catch (Exception ex)
            {
                _log($"[ОШИБКА мониторинга] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    _log($"[ОШИБКА] Внутренняя: {ex.InnerException.Message}");
                OnCompleted?.Invoke();
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task<bool> RunScriptAsync(AppConfig config)
        {
            string scriptPath = config.ScriptPath;
            try
            {
                _log($"[Скрипт] Запуск: {Path.GetFileName(scriptPath)}");

                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? ""
                };

                string ext = Path.GetExtension(scriptPath)?.ToLowerInvariant();
                if (ext == ".ps1")
                {
                    startInfo.FileName = "powershell";
                    startInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
                }
                else
                {
                    startInfo.FileName = scriptPath;
                }

                using (var process = Process.Start(startInfo))
                {
                    _log("[Скрипт] Скрипт запущен успешно.");
                    var token = _cts?.Token ?? CancellationToken.None;
                    await Task.Run(() => process?.WaitForExit(120000), token);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log($"[Скрипт] Ошибка: {ex.Message}");
                return false;
            }
        }
    }
}
