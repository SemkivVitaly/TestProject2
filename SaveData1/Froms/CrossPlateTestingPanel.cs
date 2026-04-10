using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SaveData1;
using SaveData1.CrossPlateTesting;
using SaveData1.CrossPlateTesting.Models;
using SaveData1.CrossPlateTesting.Services;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Froms
{
    public partial class CrossPlateTestingPanel : UserControl
    {
        private AppConfig _config;
        private StandRunnerService _runner;
        private ExcelReportService _excelReport;
        private Dictionary<string, string> _lastTestedByStand = new Dictionary<string, string>();

        private UsersProfile _saveUser;
        private int _saveActId;
        private string _saveActNumber = "";
        private Func<string> _getBaseStoragePath;
        private Action _onTestingProgressChanged;

        public CrossPlateTestingPanel()
        {
            InitializeComponent();
            _config = new AppConfig();
            _runner = new StandRunnerService(Log);
            _excelReport = new ExcelReportService();
            MinimumSize = new Size(520, 380);
        }

        public void RelayoutEmbeddedUi()
        {
            try
            {
                tabControl?.PerformLayout();
                panelScrollStands?.PerformLayout();
                PerformLayout();
            }
            catch { }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) return;
            BeginInvoke(new Action(RelayoutEmbeddedUi));
        }

        public void Bind(UsersProfile user, int actId, string actNumber, Func<string> getBaseStoragePath, Action onTestingProgressChanged = null)
        {
            _saveUser = user;
            _saveActId = actId;
            _saveActNumber = actNumber ?? "";
            _getBaseStoragePath = getBaseStoragePath ?? (() => "");
            _onTestingProgressChanged = onTestingProgressChanged;
            RefreshStandSerialAutocomplete();
        }

        private void RaiseTestingProgressChanged()
        {
            void go()
            {
                try { _onTestingProgressChanged?.Invoke(); } catch { }
            }
            if (InvokeRequired)
                BeginInvoke(new Action(go));
            else
                go();
        }

        private List<string> LoadCrossProductSerialsForAct()
        {
            if (_saveActId <= 0) return new List<string>();
            using (var ctx = ConnectionHelper.CreateContext())
            {
                return ctx.Product.AsNoTracking()
                    .Where(p => p.ActID == _saveActId && p.ProducType != null && p.ProducType.TypeName == CrossPlateDbHelper.CrossProductTypeName)
                    .Select(p => p.ProductSerial)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();
            }
        }

        private void RefreshStandSerialAutocomplete()
        {
            List<string> list;
            try { list = LoadCrossProductSerialsForAct(); }
            catch { list = new List<string>(); }
            foreach (Control c in scrollStands.Controls)
            {
                if (c is StandPanel sp)
                    sp.SetSerialAutocompleteSource(list);
            }
        }

        private string GetCrossExportRoot()
        {
            string basePath = _getBaseStoragePath?.Invoke()?.Trim();
            if (string.IsNullOrEmpty(basePath)) return null;
            try
            {
                return Path.Combine(basePath, $"Отгрузка_{CrossPlateDbHelper.CrossProductTypeName}_Акт_{_saveActNumber}");
            }
            catch { return null; }
        }

        private void ApplyBoundFieldsToConfig()
        {
            _config.TesterFio = _saveUser?.UserName ?? "";
            _config.ActNumber = _saveActNumber ?? "";
            string root = GetCrossExportRoot();
            _config.ExcelOutputFolder = root ?? "";
        }

        private void CrossPlateTestingPanel_Load(object sender, EventArgs e)
        {
            lblExcelFolder.Visible = false;
            txtExcelOutputFolder.Visible = false;
            btnBrowseExcelFolder.Visible = false;

            LoadConfig();
            Form pf = FindForm();
            if (pf != null)
            {
                pf.FormClosing += (s, ev) =>
                {
                    SaveConfig();
                    _excelReport?.CloseAndRelease();
                };
            }
            _runner.OnCompleted += Runner_OnCompleted;
            _runner.OnStopped += Runner_OnStopped;
            BuildDelaysTab();
        }

        private void BuildDelaysTab()
        {
            var d = _config.Delays ?? new DelaySettingsConfig();
            var tt = new ToolTip { AutoPopDelay = 15000, InitialDelay = 400 };
            var scroll = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                AutoScrollMinSize = new Size(700, 620)
            };
            int y = 10;

            var grpMav = new GroupBox { Text = "MAVLink (мс)", Location = new Point(10, y), Size = new Size(680, 270), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            y += 280;
            var grpStand = new GroupBox { Text = "Стенды (мс)", Location = new Point(10, y), Size = new Size(680, 305), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            y += 315;
            var grpScript = new GroupBox { Text = "Скрипт (мс)", Location = new Point(10, y), Size = new Size(680, 90), Anchor = AnchorStyles.Top | AnchorStyles.Left };
            y += 100;

            AddDelayRow(grpMav, 25, "Интервал heartbeat:", () => d.MavLink_HeartbeatInterval, v => d.MavLink_HeartbeatInterval = v, 5, 500, 10, tt, "Пауза между отправкой heartbeat-пакетов. Влияет на скорость установления связи с дроном.");
            AddDelayRow(grpMav, 50, "После heartbeat (чтение):", () => d.MavLink_AfterHeartbeat, v => d.MavLink_AfterHeartbeat = v, 5, 1000, 10, tt, "Ожидание после heartbeat перед запросом параметра. Слишком мало — дрон может не успеть ответить.");
            AddDelayRow(grpMav, 75, "Интервал опроса чтения:", () => d.MavLink_ReadPollInterval, v => d.MavLink_ReadPollInterval = v, 5, 200, 10, tt, "Интервал проверки ответа при чтении параметра. Меньше — быстрее получение, больше нагрузка.");
            AddDelayRow(grpMav, 100, "После heartbeat (установка):", () => d.MavLink_SetAfterHeartbeat, v => d.MavLink_SetAfterHeartbeat = v, 5, 500, 10, tt, "Ожидание после heartbeat перед командой установки параметра.");
            AddDelayRow(grpMav, 125, "После отправки (установка):", () => d.MavLink_SetAfterSend, v => d.MavLink_SetAfterSend = v, 5, 500, 10, tt, "Пауза после отправки команды установки. Даёт дрону время обработать запрос.");
            AddDelayRow(grpMav, 150, "После heartbeat (команда):", () => d.MavLink_CommandAfterHeartbeat, v => d.MavLink_CommandAfterHeartbeat = v, 5, 500, 10, tt, "Ожидание перед отправкой команд (mode, arm, disarm и т.д.).");
            AddDelayRow(grpMav, 175, "После отправки (команда):", () => d.MavLink_CommandAfterSend, v => d.MavLink_CommandAfterSend = v, 5, 500, 10, tt, "Пауза после отправки команды. Влияет на надёжность выполнения.");

            AddDelayRow(grpMav, 25, "После Arm/Disarm:", () => d.MavLink_ArmDisarmAfter, v => d.MavLink_ArmDisarmAfter = v, 5, 500, 350, tt, "Ожидание после arm/disarm перед следующим действием. Даёт мотору время среагировать.");
            AddDelayRow(grpMav, 50, "RC heartbeat:", () => d.MavLink_RcHeartbeat, v => d.MavLink_RcHeartbeat = v, 5, 200, 350, tt, "Интервал heartbeat при отправке RC override.");
            AddDelayRow(grpMav, 75, "RC после отправки:", () => d.MavLink_RcAfterSend, v => d.MavLink_RcAfterSend = v, 5, 200, 350, tt, "Пауза после отправки RC-команды.");
            AddDelayRow(grpMav, 100, "WaitFor heartbeat:", () => d.MavLink_WaitForHeartbeat, v => d.MavLink_WaitForHeartbeat = v, 5, 500, 350, tt, "Интервал heartbeat при ожидании STATUSTEXT.");
            AddDelayRow(grpMav, 125, "WaitFor опрос:", () => d.MavLink_WaitForPoll, v => d.MavLink_WaitForPoll = v, 5, 200, 350, tt, "Интервал проверки входящих сообщений при waitfor.");
            AddDelayRow(grpMav, 150, "Проверка подключения:", () => d.MavLink_CheckConnectionPoll, v => d.MavLink_CheckConnectionPoll = v, 5, 200, 350, tt, "Интервал проверки при ожидании подключения к дрону.");
            AddDelayRow(grpMav, 175, "После получения параметра:", () => d.MavLink_ParamRetrievalDelayMs, v => d.MavLink_ParamRetrievalDelayMs = v, 0, 500, 350, tt, "Пауза после успешного получения параметра перед следующей операцией.");
            AddDelayRow(grpMav, 200, "Таймаут чтения параметра (мс):", () => d.MavLink_ParamReadTimeoutMs, v => d.MavLink_ParamReadTimeoutMs = v, 1000, 60000, 350, tt, "Максимальное время ожидания ответа при чтении параметра (read, set, if, while).");

            AddDelayRow(grpStand, 25, "Стабилизация сети (тест):", () => d.Stand_NetworkStabilityTest, v => d.Stand_NetworkStabilityTest = v, 100, 10000, 10, tt, "Ожидание после подключения к Wi-Fi перед запуском скрипта при тесте одного стенда.");
            AddDelayRow(grpStand, 50, "Запуск Mission Planner:", () => d.Stand_MissionPlannerStart, v => d.Stand_MissionPlannerStart = v, 1000, 15000, 10, tt, "Ожидание после запуска Mission Planner перед работой со стендом.");
            AddDelayRow(grpStand, 75, "Стабилизация сети (без MP):", () => d.Stand_NetworkStabilityNoMp, v => d.Stand_NetworkStabilityNoMp = v, 100, 10000, 10, tt, "Ожидание при работе без Mission Planner.");
            AddDelayRow(grpStand, 100, "Пауза между стендами:", () => d.Stand_PauseBetweenStands, v => d.Stand_PauseBetweenStands = v, 100, 10000, 10, tt, "Пауза между обработкой стендов при полном цикле.");
            AddDelayRow(grpStand, 125, "Wi-Fi после подключения:", () => d.Stand_WifiAfterConnect, v => d.Stand_WifiAfterConnect = v, 500, 10000, 10, tt, "Ожидание после команды подключения к Wi-Fi перед проверкой.");
            AddDelayRow(grpStand, 150, "Повторная проверка Wi-Fi:", () => d.Stand_WifiRetryCheck, v => d.Stand_WifiRetryCheck = v, 500, 10000, 10, tt, "Дополнительное ожидание при неудачной первой проверке Wi-Fi.");
            AddDelayRow(grpStand, 175, "Шаг проверки подключения:", () => d.Stand_ConnectionCheckStep, v => d.Stand_ConnectionCheckStep = v, 200, 5000, 10, tt, "Интервал между попытками проверки подключения к дрону (Ping/UDP).");
            AddDelayRow(grpStand, 200, "Интервал сканирования мониторинга (мс):", () => d.Stand_MonitoringScanIntervalMs, v => d.Stand_MonitoringScanIntervalMs = v, 5000, 120000, 10, tt, "Пауза между итерациями сканирования в режиме мониторинга сетей.", 230);
            AddDelayRow(grpStand, 225, "Задержка между проверками сетей (мс):", () => d.Stand_NetworkScanDelayMs, v => d.Stand_NetworkScanDelayMs = v, 0, 5000, 10, tt, "Пауза между проверкой доступности каждой сети при сканировании стендов.", 230);
            AddDelayRow(grpStand, 250, "Таймер после теста (мин):", () => d.Stand_SuccessTimerMinutes, v => d.Stand_SuccessTimerMinutes = v, 1, 60, 10, tt, "Время до окрашивания панели стенда в зелёный после успешного теста. При смене серийного номера таймер сбрасывается.", 230);

            AddDelayRow(grpScript, 25, "Между узлами:", () => d.Script_BetweenNodes, v => d.Script_BetweenNodes = v, 0, 1000, 10, tt, "Пауза между выполнением узлов скрипта. Меньше — быстрее выполнение.");
            AddDelayRow(grpScript, 50, "Итерация цикла while:", () => d.Script_WhileIteration, v => d.Script_WhileIteration = v, 0, 1000, 10, tt, "Пауза между итерациями цикла while.");

            var btnReset = new Button { Text = "Сбросить по умолчанию", Location = new Point(10, y + 10), Size = new Size(160, 28) };
            btnReset.Click += (s, ev) =>
            {
                _config.Delays = new DelaySettingsConfig();
                DelaySettings.Apply(_config.Delays);
                SaveConfig();
                BuildDelaysTab();
            };

            scroll.Controls.Add(grpMav);
            scroll.Controls.Add(grpStand);
            scroll.Controls.Add(grpScript);
            scroll.Controls.Add(btnReset);

            tabDelays.Controls.Clear();
            tabDelays.Controls.Add(scroll);
        }

        private void AddDelayRow(GroupBox grp, int y, string label, System.Func<int> getter, System.Action<int> setter, int min, int max, int x = 10, ToolTip tt = null, string tooltipText = null, int numXOffset = 220)
        {
            var lbl = new Label { Text = label, Location = new Point(x, y + 2), AutoSize = true };
            var num = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, getter())),
                Width = 80,
                Location = new Point(x + numXOffset, y)
            };
            if (tt != null && !string.IsNullOrEmpty(tooltipText))
            {
                tt.SetToolTip(lbl, tooltipText);
                tt.SetToolTip(num, tooltipText);
            }
            num.ValueChanged += (s, ev) =>
            {
                setter((int)((NumericUpDown)s).Value);
                DelaySettings.Apply(_config.Delays);
                SaveConfig();
            };
            grp.Controls.Add(lbl);
            grp.Controls.Add(num);
        }

        private void LoadConfig()
        {
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                    _config = CrossPlateConfigService.Load(ctx, _saveActNumber);
            }
            catch
            {
                _config = new AppConfig();
            }

            txtMissionPlannerPath.Text = _config.MissionPlannerPath ?? "";
            txtScriptPath.Text = _config.ScriptPath ?? "";
            chkMonitoringMode.Checked = _config.MonitoringModeEnabled;
            chkSkipConnectionCheck.Checked = _config.SkipMavLinkConnectionCheck;
            ApplyBoundFieldsToConfig();
            txtExcelOutputFolder.Text = _config.ExcelOutputFolder ?? "";
            txtDronePing.Text = !string.IsNullOrWhiteSpace(_config.DronePingAddress) ? _config.DronePingAddress : "192.168.4.1;192.168.1.1";
            numDronePort.Value = _config.DronePort > 0 ? Math.Min(65535, _config.DronePort) : 14550;
            numTimeout.Value = Math.Max(5, Math.Min(120, _config.ConnectionTimeoutSeconds));
            if (_config.DroneBridge == null)
                _config.DroneBridge = new DroneBridgeConfig();
            if (_config.Delays == null)
                _config.Delays = new DelaySettingsConfig();
            DelaySettings.Apply(_config.Delays);

            scrollStands.Controls.Clear();
            if (_config.Stands != null)
            {
                foreach (var stand in _config.Stands)
                {
                    AddStandControl(stand);
                }
            }
            RefreshStandSerialAutocomplete();
        }

        private void SaveConfig()
        {
            _config.MissionPlannerPath = txtMissionPlannerPath.Text?.Trim() ?? "";
            _config.ScriptPath = txtScriptPath.Text?.Trim() ?? "";
            _config.MonitoringModeEnabled = chkMonitoringMode.Checked;
            _config.SkipMavLinkConnectionCheck = chkSkipConnectionCheck.Checked;
            _config.DronePingAddress = txtDronePing.Text?.Trim() ?? "192.168.4.1;192.168.1.1";
            _config.DronePort = (int)numDronePort.Value;
            _config.ConnectionTimeoutSeconds = (int)numTimeout.Value;
            CollectStandsFromUI();
            ApplyBoundFieldsToConfig();
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                    CrossPlateConfigService.Save(ctx, _saveActNumber, _config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void CollectStandsFromUI()
        {
            _config.Stands.Clear();
            foreach (Control c in scrollStands.Controls)
            {
                if (c is StandPanel sp)
                {
                    var stand = sp.Stand;
                    stand.Name = sp.txtName.Text?.Trim() ?? "Стенд";
                    stand.WifiSsid = sp.txtSsid.Text?.Trim() ?? "";
                    stand.WifiPassword = sp.txtPassword.Text ?? "";
                    stand.HasSavedCredentials = !string.IsNullOrWhiteSpace(stand.WifiSsid);
                    stand.ProductSerialNumber = sp.txtName?.Text?.Trim() ?? "";
                    _config.Stands.Add(stand);
                }
            }
        }

        private void btnAddStand_Click(object sender, EventArgs e)
        {
            var stand = new Stand();
            _config.Stands.Add(stand);
            AddStandControl(stand);
            RefreshStandSerialAutocomplete();
        }

        private void AddStandControl(Stand stand)
        {
            var panel = new StandPanel(stand, RemoveStand, Log, SaveConfig, RunTestForStand, OnDefectClick);
            scrollStands.Controls.Add(panel);
        }

        private void TryRecordCrossSuccess(Stand stand)
        {
            string ser = (stand.ProductSerialNumber ?? stand.Name ?? "").Trim();
            if (string.IsNullOrEmpty(ser) || _saveUser == null) return;
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    if (!CrossPlateDbHelper.TryGetCrossProduct(ctx, _saveActId, ser, out var prod))
                    {
                        void showErr()
                        {
                            MessageBox.Show(
                                $"С/Н «{ser}» не найден в акте с типом «{CrossPlateDbHelper.CrossProductTypeName}».",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        if (InvokeRequired) BeginInvoke(new Action(showErr));
                        else showErr();
                        return;
                    }
                    CrossPlateDbHelper.RecordSuccess(ctx, prod.ProductID, _saveUser.UserID, out _);
                }
                string folder = GetCrossExportRoot();
                if (string.IsNullOrEmpty(folder)) return;
                Directory.CreateDirectory(folder);
                ApplyBoundFieldsToConfig();
                _excelReport.EnsureWorkbook(folder, _saveActNumber, _saveUser.UserName ?? "");
                int testNum = _excelReport.GetNextSuccessTestNumber();
                _excelReport.AddSuccessRecord(stand, testNum);
                RaiseTestingProgressChanged();
            }
            catch (Exception ex)
            {
                Log($"[Ошибка записи теста] {ex.Message}");
            }
        }

        private void OnDefectClick(StandPanel panel)
        {
            SaveConfig();
            string serial = (panel.Stand.ProductSerialNumber ?? panel.Stand.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(serial))
            {
                MessageBox.Show("Укажите серийный номер.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int productId;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                if (!CrossPlateDbHelper.TryGetCrossProduct(ctx, _saveActId, serial, out var product))
                {
                    MessageBox.Show(
                        $"Продукт «{serial}» не найден в акте с типом «{CrossPlateDbHelper.CrossProductTypeName}».",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                productId = product.ProductID;
            }
            string folder = GetCrossExportRoot();
            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show("На вкладке «Полетники (USB)» укажите корневую папку сохранения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { Directory.CreateDirectory(folder); } catch { }
            try
            {
                ApplyBoundFieldsToConfig();
                _excelReport.EnsureWorkbook(folder, _saveActNumber, _saveUser?.UserName ?? "");
                _excelReport.AddDefectRecord(panel.Stand, _saveActNumber, _saveUser?.UserName ?? "");
                Log($"[Неисправность] Записано в Excel: {serial}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи в Excel: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int tflightId;
            int nonConformityErrorId;
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    tflightId = CrossPlateDbHelper.CreateFailedTestSession(ctx, productId, _saveUser.UserID);
                    nonConformityErrorId = CrossPlateDbHelper.CreateNonConformityError(ctx, productId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи несоответствия в БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RaiseTestingProgressChanged();

            NonConformityLabelHelper.OfferGenerateLabel(
                nonConformityErrorId,
                _saveUser?.UserName ?? "",
                CrossPlateDbHelper.CrossPlateDefectDescriptionText);

            var owner = FindForm();
            var errorForm = new FlightTestErrorForm(
                productId, tflightId, serial, _saveActNumber, _saveUser, () => RaiseTestingProgressChanged(), null, nonConformityErrorId);
            errorForm.Show(owner);
        }

        private void btnBrowseExcelFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Выберите папку для Excel-отчётов";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtExcelOutputFolder.Text = dlg.SelectedPath;
                }
            }
        }

        private bool ValidateRequiredForTest(out string error, Stand stand = null)
        {
            string b = _getBaseStoragePath?.Invoke()?.Trim();
            if (string.IsNullOrWhiteSpace(b) || !Directory.Exists(b))
            {
                error = "На вкладке «Полетники (USB)» укажите существующую корневую папку сохранения.";
                return false;
            }
            if (stand != null)
            {
                if (string.IsNullOrWhiteSpace(stand.ProductSerialNumber ?? stand.Name ?? ""))
                {
                    error = "Укажите серийный номер стенда.";
                    return false;
                }
            }
            else if (_config.Stands == null || _config.Stands.Count == 0)
            {
                error = "Добавьте стенды и укажите серийные номера.";
                return false;
            }
            else if (!_config.Stands.Exists(s => !string.IsNullOrWhiteSpace(s.ProductSerialNumber ?? s.Name)))
            {
                error = "Укажите серийный номер хотя бы для одного стенда.";
                return false;
            }
            error = null;
            return true;
        }

        private async void RunTestForStand(StandPanel panel)
        {
            SaveConfig();
            if (!ValidateRequiredForTest(out string err, panel.Stand))
            {
                MessageBox.Show(err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            panel.SetTestRunning(true);
            bool success = await _runner.RunSingleStandAsync(_config, panel.Stand);
            panel.SetTestRunning(false);
            ApplyBoundFieldsToConfig();
            if (!string.IsNullOrWhiteSpace(_config.ExcelOutputFolder))
            {
                BridgeUnifiedLogSaveService.TrySaveAfterStandTest(
                    _config.ExcelOutputFolder,
                    panel.Stand,
                    _config.Esp32BridgeWebHost,
                    _config.Esp32BridgeWebPort,
                    _config.Esp32BridgeLogTimeoutMs,
                    Log);
            }
            if (success)
            {
                TryRecordCrossSuccess(panel.Stand);
                panel.OnTestSuccess();
            }
        }

        private void RemoveStand(StandPanel panel)
        {
            scrollStands.Controls.Remove(panel);
            _config.Stands.RemoveAll(s => s.Id == panel.Stand.Id);
            SaveConfig();
        }

        private void btnBrowseMissionPlanner_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*";
                dlg.Title = "Выберите Mission Planner";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtMissionPlannerPath.Text = dlg.FileName;
                }
            }
        }

        private void btnBrowseScript_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Скрипты параметров (*.mavparams)|*.mavparams|Скрипты (*.bat;*.cmd;*.ps1)|*.bat;*.cmd;*.ps1|Все файлы (*.*)|*.*";
                dlg.Title = "Выберите скрипт";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtScriptPath.Text = dlg.FileName;
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            SaveConfig();
            if (!ValidateRequiredForTest(out string err))
            {
                MessageBox.Show(err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            SetAllTestButtonsEnabled(false);

            Action<Stand, bool> onTestComplete = null;
            if (!string.IsNullOrWhiteSpace(_config.ExcelOutputFolder))
            {
                try
                {
                    _excelReport.EnsureWorkbook(_config.ExcelOutputFolder, _config.ActNumber ?? "", _config.TesterFio ?? "");
                    onTestComplete = OnTestComplete;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия Excel: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    SetAllTestButtonsEnabled(true);
                    return;
                }
            }

            if (_config.MonitoringModeEnabled)
            {
                _lastTestedByStand.Clear();
                try
                {
                    await _runner.RunMonitoringAsync(_config, _lastTestedByStand, onTestComplete);
                }
                catch (Exception ex)
                {
                    Log($"[ОШИБКА мониторинга] {ex.GetType().Name}: {ex.Message}");
                }
            }
            else
            {
                await _runner.RunAsync(_config, onTestComplete);
            }
        }

        private void OnTestComplete(Stand stand, bool success)
        {
            ApplyBoundFieldsToConfig();
            if (!string.IsNullOrWhiteSpace(_config.ExcelOutputFolder))
            {
                BridgeUnifiedLogSaveService.TrySaveAfterStandTest(
                    _config.ExcelOutputFolder,
                    stand,
                    _config.Esp32BridgeWebHost,
                    _config.Esp32BridgeWebPort,
                    _config.Esp32BridgeLogTimeoutMs,
                    Log);
            }
            if (!success) return;
            var panel = FindPanelByStand(stand);
            panel?.OnTestSuccess();
            TryRecordCrossSuccess(stand);
        }

        private StandPanel FindPanelByStand(Stand stand)
        {
            if (stand == null) return null;
            foreach (Control c in scrollStands.Controls)
            {
                if (c is StandPanel sp && sp.Stand.Id == stand.Id)
                    return sp;
            }
            return null;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _runner.Stop();
        }

        private void Runner_OnCompleted()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Runner_OnCompleted));
                return;
            }
            _excelReport?.CloseAndRelease();
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            SetAllTestButtonsEnabled(true);
        }

        private void SetAllTestButtonsEnabled(bool enabled)
        {
            foreach (Control c in scrollStands.Controls)
            {
                if (c is StandPanel sp)
                    sp.SetTestRunning(!enabled);
            }
        }

        private void Runner_OnStopped()
        {
            Runner_OnCompleted();
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Log), message);
                return;
            }
            string line = $"{message}{Environment.NewLine}";
            txtLog.AppendText(line);
            txtLog.ScrollToCaret();
        }

        private void btnScriptGenerator_Click(object sender, EventArgs e)
        {
            using (var form = new ScriptGeneratorForm())
            {
                form.ShowDialog(this);
            }
        }
    }

    /// <summary>
    /// Панель одного стенда
    /// </summary>
    internal class StandPanel : Panel
    {

        public Stand Stand { get; }
        internal readonly TextBox txtName;
        internal readonly TextBox txtSsid;
        internal readonly TextBox txtPassword;
        private readonly Button btnSaveFromCurrent;
        private readonly Button btnSaveManual;
        private readonly Button btnRemove;
        private readonly Button btnTest;
        private readonly Button btnDefect;
        private System.Windows.Forms.Timer _successTimer;
        private string _lastTestedSerialNumber;
        private readonly Label _lblTimer;
        private int _remainingSeconds;
        private List<string> _serialAutocompleteSerials = new List<string>();
        private bool _updatingSerialFromAutocomplete;

        public StandPanel(Stand stand, Action<StandPanel> onRemove, Action<string> onLog, Action onSaveConfig, Action<StandPanel> onRunTest, Action<StandPanel> onDefect)
        {
            Stand = stand;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(5);
            Size = new Size(600, 77);
            Margin = new Padding(0, 0, 5, 5);
            BackColor = Color.White;

            int y = 5;
            int x = 5;

            var lblName = new Label { Text = "Серийный\nномер:", Location = new Point(x, y + 2), AutoSize = true };
            txtName = new TextBox
            {
                Text = stand.ProductSerialNumber ?? stand.Name ?? "",
                Location = new Point(x + 70, y),
                Size = new Size(120, 20),
                Tag = "name"
            };
            txtName.TextChanged += TxtName_AutoCompleteNormalize_TextChanged;
            txtName.TextChanged += TxtName_TextChanged;
            Controls.Add(lblName);
            Controls.Add(txtName);
            x += 200;

            var lblSsid = new Label { Text = "Wi-Fi:", Location = new Point(x, y + 2), AutoSize = true };
            txtSsid = new TextBox
            {
                Text = stand.WifiSsid ?? "",
                Location = new Point(x + 45, y),
                Size = new Size(100, 20),
                Tag = "ssid"
            };
            Controls.Add(lblSsid);
            Controls.Add(txtSsid);
            x += 155;

            var lblPwd = new Label { Text = "Пароль:", Location = new Point(x, y + 2), AutoSize = true };
            txtPassword = new TextBox
            {
                Text = stand.HasSavedCredentials ? "********" : "",
                Location = new Point(x + 50, y),
                Size = new Size(80, 20),
                UseSystemPasswordChar = true,
                PasswordChar = '*',
                Tag = "pwd"
            };
            if (stand.HasSavedCredentials)
                txtPassword.Text = stand.WifiPassword;
            Controls.Add(lblPwd);
            Controls.Add(txtPassword);
            x += 135;

            _lblTimer = new Label
            {
                Text = "",
                Location = new Point(x, y + 2),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.25f)
            };
            Controls.Add(_lblTimer);

            y += 28;
            x = 5;

            btnSaveFromCurrent = new Button
            {
                Text = "Сохранить из текущего Wi-Fi",
                Location = new Point(x, y),
                Size = new Size(160, 26),
                UseVisualStyleBackColor = true
            };
            btnSaveFromCurrent.Click += (s, ev) =>
            {
                var (ssid, pwd) = WifiInfoService.GetCurrentWifiInfo();
                if (!string.IsNullOrEmpty(ssid))
                {
                    txtSsid.Text = ssid;
                    txtPassword.Text = pwd ?? "";
                    Stand.Name = txtName.Text?.Trim() ?? "Стенд";
                    Stand.WifiSsid = ssid;
                    Stand.WifiPassword = pwd ?? "";
                    Stand.HasSavedCredentials = true;
                    onLog($"[Сохранено] Серийный номер '{Stand.Name}': Wi-Fi '{ssid}' (пароль скрыт)");
                    if (string.IsNullOrEmpty(pwd))
                    {
                        MessageBox.Show("Пароль не получен. Возможно, требуется запуск от имени администратора.\nСохраните пароль вручную.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    onSaveConfig?.Invoke();
                }
                else
                {
                    MessageBox.Show("Не удалось получить данные текущего Wi-Fi. Подключитесь к сети стенда и повторите.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            Controls.Add(btnSaveFromCurrent);
            x += 165;

            btnSaveManual = new Button
            {
                Text = "Сохранить вручную",
                Location = new Point(x, y),
                Size = new Size(120, 26),
                UseVisualStyleBackColor = true
            };
            btnSaveManual.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtSsid.Text))
                {
                    MessageBox.Show("Введите название Wi-Fi (SSID).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Stand.WifiSsid = txtSsid.Text.Trim();
                Stand.WifiPassword = txtPassword?.Text ?? "";
                Stand.HasSavedCredentials = true;
                Stand.Name = txtName.Text?.Trim() ?? "Стенд";
                onLog($"[Сохранено] Серийный номер '{Stand.Name}': Wi-Fi '{Stand.WifiSsid}' (пароль скрыт)");
                onSaveConfig?.Invoke();
            };
            Controls.Add(btnSaveManual);
            x += 125;

            btnRemove = new Button
            {
                Text = "Удалить",
                Location = new Point(x, y),
                Size = new Size(70, 26),
                UseVisualStyleBackColor = true,
                ForeColor = Color.DarkRed
            };
            btnRemove.Click += (s, ev) =>
            {
                if (MessageBox.Show($"Удалить запись '{stand.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    onRemove(this);
                }
            };
            Controls.Add(btnRemove);
            x += 75;

            btnTest = new Button
            {
                Text = "Тест",
                Location = new Point(x, y),
                Size = new Size(70, 26),
                UseVisualStyleBackColor = true,
                BackColor = Color.FromArgb(80, 120, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnTest.Click += (s, ev) =>
            {
                Stand.Name = txtName.Text?.Trim() ?? "Стенд";
                Stand.WifiSsid = txtSsid.Text?.Trim() ?? "";
                Stand.WifiPassword = txtPassword?.Text ?? "";
                Stand.HasSavedCredentials = !string.IsNullOrWhiteSpace(Stand.WifiSsid);
                Stand.ProductSerialNumber = txtName?.Text?.Trim() ?? "";
                if (!Stand.HasSavedCredentials)
                {
                    MessageBox.Show("Сначала сохраните данные Wi-Fi для этого стенда.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                onRunTest?.Invoke(this);
            };
            Controls.Add(btnTest);
            x += 75;

            btnDefect = new Button
            {
                Text = "Неисправность",
                Location = new Point(x, y),
                Size = new Size(100, 26),
                UseVisualStyleBackColor = true,
                BackColor = Color.FromArgb(180, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDefect.Click += (s, ev) =>
            {
                Stand.Name = txtName.Text?.Trim() ?? "Стенд";
                Stand.ProductSerialNumber = txtName?.Text?.Trim() ?? "";
                onDefect?.Invoke(this);
            };
            Controls.Add(btnDefect);
        }

        /// <summary>Серийные номера продуктов «Кросс-плата» текущего акта — подсказки при вводе (как у полетников).</summary>
        public void SetSerialAutocompleteSource(IEnumerable<string> serials)
        {
            _serialAutocompleteSerials = serials?.Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList() ?? new List<string>();
            var src = new AutoCompleteStringCollection();
            foreach (var s in _serialAutocompleteSerials)
                src.Add(s);
            var added = new HashSet<string>();
            foreach (var s in _serialAutocompleteSerials)
            {
                string last4 = GetLast4DigitsForAutocomplete(s);
                if (!string.IsNullOrEmpty(last4))
                {
                    string key = last4 + " - " + s;
                    if (added.Add(key))
                        src.Add(key);
                }
            }
            if (txtName.InvokeRequired)
            {
                txtName.Invoke(new Action(() =>
                {
                    txtName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    txtName.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    txtName.AutoCompleteCustomSource = src;
                }));
                return;
            }
            txtName.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtName.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtName.AutoCompleteCustomSource = src;
        }

        private static string GetLast4DigitsForAutocomplete(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 4) return null;
            var digits = new string(s.Reverse().TakeWhile(char.IsDigit).Take(4).Reverse().ToArray());
            return digits.Length == 4 ? digits : null;
        }

        private void TxtName_AutoCompleteNormalize_TextChanged(object sender, EventArgs e)
        {
            if (_updatingSerialFromAutocomplete) return;
            string t = txtName.Text;
            if (string.IsNullOrEmpty(t) || !t.Contains(" - ")) return;
            int idx = t.LastIndexOf(" - ", StringComparison.Ordinal);
            string afterDash = t.Substring(idx + 3).Trim();
            string canonical = _serialAutocompleteSerials.FirstOrDefault(s => string.Equals(s, afterDash, StringComparison.OrdinalIgnoreCase));
            if (canonical != null)
            {
                _updatingSerialFromAutocomplete = true;
                txtName.Text = canonical;
                txtName.SelectAll();
                _updatingSerialFromAutocomplete = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                StopSuccessTimer();
            base.Dispose(disposing);
        }

        public void SetTestRunning(bool running)
        {
            if (btnTest.InvokeRequired)
            {
                btnTest.Invoke(new Action<bool>(SetTestRunning), running);
                return;
            }
            btnTest.Enabled = !running;
            btnTest.Text = running ? "..." : "Тест";
        }

        /// <summary>
        /// Вызывается после успешного выполнения теста — запускает таймер на 2 минуты.
        /// </summary>
        public void OnTestSuccess()
        {
            StopSuccessTimer();
            _lastTestedSerialNumber = txtName?.Text?.Trim() ?? "";
            _remainingSeconds = DelaySettings.Stand_SuccessTimerMinutes * 60;
            UpdateTimerLabel();
            _successTimer = new System.Windows.Forms.Timer();
            _successTimer.Interval = 1000;
            _successTimer.Tick += SuccessTimer_Tick;
            _successTimer.Start();
        }

        private void SuccessTimer_Tick(object sender, EventArgs e)
        {
            _remainingSeconds--;
            UpdateTimerLabel();
            if (_remainingSeconds <= 0)
            {
                StopSuccessTimer();
                if (InvokeRequired)
                    BeginInvoke(new Action(SetGreenColor));
                else
                    SetGreenColor();
            }
        }

        private void UpdateTimerLabel()
        {
            if (_lblTimer == null) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateTimerLabel));
                return;
            }
            if (_remainingSeconds > 0)
            {
                int min = _remainingSeconds / 60;
                int sec = _remainingSeconds % 60;
                _lblTimer.Text = $"Осталось: {min}:{sec:D2}";
                _lblTimer.Visible = true;
            }
            else
            {
                _lblTimer.Text = "";
                _lblTimer.Visible = false;
            }
        }

        private void SetGreenColor()
        {
            BackColor = Color.FromArgb(200, 255, 200);
            _lblTimer.Text = "";
            _lblTimer.Visible = false;
        }

        private void ResetToWhite()
        {
            StopSuccessTimer();
            BackColor = Color.White;
            _lblTimer.Text = "";
            _lblTimer.Visible = false;
        }

        private void StopSuccessTimer()
        {
            _successTimer?.Stop();
            _successTimer?.Dispose();
            _successTimer = null;
            _remainingSeconds = 0;
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            string current = txtName?.Text?.Trim() ?? "";
            Stand.ProductSerialNumber = current;
            if (!string.IsNullOrEmpty(_lastTestedSerialNumber) && !string.Equals(current, _lastTestedSerialNumber, StringComparison.Ordinal))
            {
                _lastTestedSerialNumber = null;
                ResetToWhite();
            }
        }
    }
}
