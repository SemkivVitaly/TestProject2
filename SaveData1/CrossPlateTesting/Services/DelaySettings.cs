using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Глобальные настройки задержек. Применяются при загрузке конфигурации.
    /// </summary>
    public static class DelaySettings
    {
        private static DelaySettingsConfig _d = new DelaySettingsConfig();

        public static void Apply(DelaySettingsConfig config)
        {
            _d = config ?? new DelaySettingsConfig();
        }

        public static DelaySettingsConfig Current => _d;

        // MAVLink
        public static int MavLink_HeartbeatInterval => _d.MavLink_HeartbeatInterval;
        public static int MavLink_AfterHeartbeat => _d.MavLink_AfterHeartbeat;
        public static int MavLink_ReadPollInterval => _d.MavLink_ReadPollInterval;
        public static int MavLink_SetAfterHeartbeat => _d.MavLink_SetAfterHeartbeat;
        public static int MavLink_SetAfterSend => _d.MavLink_SetAfterSend;
        public static int MavLink_CommandAfterHeartbeat => _d.MavLink_CommandAfterHeartbeat;
        public static int MavLink_CommandAfterSend => _d.MavLink_CommandAfterSend;
        public static int MavLink_ArmDisarmAfter => _d.MavLink_ArmDisarmAfter;
        public static int MavLink_RcHeartbeat => _d.MavLink_RcHeartbeat;
        public static int MavLink_RcAfterSend => _d.MavLink_RcAfterSend;
        public static int MavLink_WaitForHeartbeat => _d.MavLink_WaitForHeartbeat;
        public static int MavLink_WaitForPoll => _d.MavLink_WaitForPoll;
        public static int MavLink_CheckConnectionPoll => _d.MavLink_CheckConnectionPoll;
        public static int MavLink_ParamRetrievalDelayMs => _d.MavLink_ParamRetrievalDelayMs;
        public static int MavLink_ParamReadTimeoutMs => _d.MavLink_ParamReadTimeoutMs;

        // StandRunner
        public static int Stand_NetworkStabilityTest => _d.Stand_NetworkStabilityTest;
        public static int Stand_MissionPlannerStart => _d.Stand_MissionPlannerStart;
        public static int Stand_NetworkStabilityNoMp => _d.Stand_NetworkStabilityNoMp;
        public static int Stand_PauseBetweenStands => _d.Stand_PauseBetweenStands;
        public static int Stand_WifiAfterConnect => _d.Stand_WifiAfterConnect;
        public static int Stand_WifiRetryCheck => _d.Stand_WifiRetryCheck;
        public static int Stand_ConnectionCheckStep => _d.Stand_ConnectionCheckStep;
        public static int Stand_NetworkScanDelayMs => _d.Stand_NetworkScanDelayMs;
        public static int Stand_SuccessTimerMinutes => _d.Stand_SuccessTimerMinutes;

        // ScriptExecutor
        public static int Script_BetweenNodes => _d.Script_BetweenNodes;
        public static int Script_WhileIteration => _d.Script_WhileIteration;
        public static int Stand_MonitoringScanIntervalMs => _d.Stand_MonitoringScanIntervalMs;
    }
}
