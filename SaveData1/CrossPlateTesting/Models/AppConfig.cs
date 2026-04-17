using System.Collections.Generic;

namespace SaveData1.CrossPlateTesting.Models
{
    /// <summary>
    /// Рекомендуемые настройки DroneBridge для оптимальной работы MAVLink.
    /// Задаются в веб-интерфейсе DroneBridge на устройстве.
    /// </summary>
    public class DroneBridgeConfig
    {
        public int UartRtsThreshold { get; set; } = 64;
        public string UartSerialProtocol { get; set; } = "MavLink";
        public int MaxPacketSize { get; set; } = 128;
        public int SerialReadTimeoutMs { get; set; } = 50;
        public int WifiChannel { get; set; } = 6;
    }

    /// <summary>
    /// Настройки задержек (мс) для управления скоростью работы приложения.
    /// </summary>
    public class DelaySettingsConfig
    {
        // MAVLink
        public int MavLink_HeartbeatInterval { get; set; } = 30;
        public int MavLink_AfterHeartbeat { get; set; } = 120;
        public int MavLink_ReadPollInterval { get; set; } = 20;
        public int MavLink_SetAfterHeartbeat { get; set; } = 80;
        public int MavLink_SetAfterSend { get; set; } = 100;
        public int MavLink_CommandAfterHeartbeat { get; set; } = 60;
        public int MavLink_CommandAfterSend { get; set; } = 80;
        public int MavLink_ArmDisarmAfter { get; set; } = 120;
        public int MavLink_RcHeartbeat { get; set; } = 25;
        public int MavLink_RcAfterSend { get; set; } = 40;
        public int MavLink_WaitForHeartbeat { get; set; } = 80;
        public int MavLink_WaitForPoll { get; set; } = 20;
        public int MavLink_CheckConnectionPoll { get; set; } = 40;
        public int MavLink_ParamRetrievalDelayMs { get; set; } = 50;
        public int MavLink_ParamReadTimeoutMs { get; set; } = 15000;

        // StandRunner
        public int Stand_NetworkStabilityTest { get; set; } = 1500;
        public int Stand_MissionPlannerStart { get; set; } = 3000;
        public int Stand_NetworkStabilityNoMp { get; set; } = 1500;
        public int Stand_PauseBetweenStands { get; set; } = 1000;
        public int Stand_WifiAfterConnect { get; set; } = 2000;
        public int Stand_WifiRetryCheck { get; set; } = 2500;
        public int Stand_ConnectionCheckStep { get; set; } = 800;
        public int Stand_NetworkScanDelayMs { get; set; } = 500;
        /// <summary>Таймер после успешного теста (мин). По истечении панель стенда окрашивается в зелёный.</summary>
        public int Stand_SuccessTimerMinutes { get; set; } = 2;

        // ScriptExecutor
        public int Script_BetweenNodes { get; set; } = 60;
        public int Script_WhileIteration { get; set; } = 60;
        public int Stand_MonitoringScanIntervalMs { get; set; } = 15000;
    }

    /// <summary>
    /// Конфигурация приложения для сохранения
    /// </summary>
    public class AppConfig
    {
        public List<Stand> Stands { get; set; } = new List<Stand>();
        public string MissionPlannerPath { get; set; } = "";
        public string ScriptPath { get; set; } = "";
        public string DronePingAddress { get; set; } = "192.168.4.1;192.168.1.1";
        public int DronePort { get; set; } = 14550;
        public int ConnectionTimeoutSeconds { get; set; } = 15;
        public bool MonitoringModeEnabled { get; set; }
        /// <summary>Выполнять тесты без проверки MAVLink. Используются Ping/UDP для информации, скрипт выполняется в любом случае.</summary>
        public bool SkipMavLinkConnectionCheck { get; set; }
        /// <summary>
        /// Отправлять MAVLink-пакеты в формате v2 (STX=0xFD). По умолчанию false (v1, совместимо со всеми прошивками).
        /// Включите, если дрон настроен на режим v2-only (ArduPilot с SERIAL*_PROTOCOL force-v2 или PX4 MAV_PROTO_VER=2)
        /// и не реагирует на v1-пакеты. На приём мы всегда понимаем оба формата.
        /// </summary>
        public bool UseMavLinkV2 { get; set; } = false;
        public string ExcelOutputFolder { get; set; } = "";
        public string TesterFio { get; set; } = "";
        public string ActNumber { get; set; } = "";
        /// <summary>Хост веб-интерфейса ESP32 DroneBridge для скачивания единого лога после теста.</summary>
        public string Esp32BridgeWebHost { get; set; } = "192.168.2.1";
        /// <summary>Порт HTTP API моста (для URL /api/log/file).</summary>
        public int Esp32BridgeWebPort { get; set; } = 80;
        /// <summary>Таймаут HTTP при скачивании /api/log/file (мс).</summary>
        public int Esp32BridgeLogTimeoutMs { get; set; } = 20000;
        /// <summary>Рекомендуемые настройки DroneBridge (для справки при настройке устройства).</summary>
        public DroneBridgeConfig DroneBridge { get; set; } = new DroneBridgeConfig();
        /// <summary>Задержки (мс) для настройки скорости работы.</summary>
        public DelaySettingsConfig Delays { get; set; } = new DelaySettingsConfig();
    }
}
