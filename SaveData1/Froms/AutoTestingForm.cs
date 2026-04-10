using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SaveData1;
using SaveData1.Helpers;
using SaveData1.Entity;

namespace SaveData1.Froms
{
    public partial class AutoTestingForm : Form
    {
        private bool _isStarted;
        private readonly UsersProfile _user;
        private readonly string _actNumber;
        private readonly int _actId;
        private readonly bool _actHasCrossPlata;
        private readonly string _productTypeName = "Полетники";
        private List<string> _productsForAutocomplete = new List<string>();
        private List<ProductsWithErrorsItem> _productsWithErrorsList = new List<ProductsWithErrorsItem>();
        private CrossPlateTestingPanel _crossPanel;
        private readonly bool _crossOnlyMode;
        private TextBox _txtFolderCrossRoot;
        /// <summary>Обновление списка продуктов на форме сотрудника (актуальный прогресс теста / ярлыки).</summary>
        private readonly Action _onEmployeeProductsRefresh;

        public AutoTestingForm(UsersProfile user, string actNumber, int actId, bool actHasCrossPlata, bool crossOnlyMode = false, Action onEmployeeProductsRefresh = null)
        {
            InitializeComponent();
            _user = user;
            _actNumber = actNumber;
            _actId = actId;
            _actHasCrossPlata = actHasCrossPlata;
            _crossOnlyMode = crossOnlyMode;
            _onEmployeeProductsRefresh = onEmployeeProductsRefresh;

            lblFioAct.Text = $"{user?.UserName ?? ""} | Акт № {actNumber}";

            if (!_actHasCrossPlata)
                tabMain.TabPages.Remove(tabCrossPlate);
            else if (_crossOnlyMode)
                tabMain.TabPages.Remove(tabPoletniki);

            Load += AutoTestingForm_Load;
            FormClosing += AutoTestingForm_FormClosing;
            if (!_crossOnlyMode)
                tabMain.SelectedIndexChanged += TabMain_SelectedIndexChanged;

            if (_actHasCrossPlata)
            {
                _crossPanel = new CrossPlateTestingPanel();
                _crossPanel.Dock = DockStyle.Fill;
                _crossPanel.Bind(user, actId, actNumber, () => ActiveFolderPathText, NotifyEmployeeProductsRefresh);
                pnlCrossHost.Controls.Add(_crossPanel);
            }

            if (_crossOnlyMode && _actHasCrossPlata)
            {
                Text = "Тестирование кросс-плат";
                ApplyCrossOnlyStandaloneLayout(CreateCrossOnlyFolderBarPanel());
            }

            LoadSettings();
            if (!_crossOnlyMode)
            {
                LoadProductsForAutocomplete();
                LoadProductsWithErrors();
            }
        }

        private void NotifyEmployeeProductsRefresh()
        {
            if (_onEmployeeProductsRefresh == null) return;
            try
            {
                void invoke()
                {
                    try { _onEmployeeProductsRefresh(); } catch { /* не рвём форму теста */ }
                }
                if (InvokeRequired)
                    BeginInvoke(new Action(invoke));
                else
                    invoke();
            }
            catch { }
        }

        private string ActiveFolderPathText => (_txtFolderCrossRoot ?? txtFolderPath).Text;

        /// <summary>Верхняя панель «корневая папка» только для режима только кросс-плат (без вкладок).</summary>
        private Panel CreateCrossOnlyFolderBarPanel()
        {
            var bar = new Panel { Height = 44, Padding = new Padding(4, 8, 4, 4) };
            var lblPath = new Label { Text = "Папка сохранения (корень):", AutoSize = true, Location = new Point(8, 14) };
            var lblInfo = new Label
            {
                Text = $"{_user?.UserName ?? ""} | Акт № {_actNumber}",
                AutoSize = true
            };
            _txtFolderCrossRoot = new TextBox { Location = new Point(175, 10), Width = 400, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var btn = new Button { Text = "Обзор...", Size = new Size(72, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            void positionBrowse()
            {
                btn.Left = bar.ClientSize.Width - btn.Width - 8;
                lblInfo.Left = Math.Max(_txtFolderCrossRoot.Left + 120, btn.Left - lblInfo.Width - 10);
                lblInfo.Top = 14;
                _txtFolderCrossRoot.Width = Math.Max(120, lblInfo.Left - _txtFolderCrossRoot.Left - 8);
            }
            bar.Resize += (s, ev) => positionBrowse();
            btn.Click += (s, ev) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Выберите корневую папку для отчётов";
                    if (!string.IsNullOrEmpty(_txtFolderCrossRoot.Text) && Directory.Exists(_txtFolderCrossRoot.Text))
                        fbd.SelectedPath = _txtFolderCrossRoot.Text;
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        _txtFolderCrossRoot.Text = fbd.SelectedPath;
                        SaveSettings(fbd.SelectedPath);
                    }
                }
            };
            bar.Controls.Add(lblPath);
            bar.Controls.Add(_txtFolderCrossRoot);
            bar.Controls.Add(btn);
            bar.Controls.Add(lblInfo);
            positionBrowse();
            return bar;
        }

        /// <summary>Убирает TabControl и вкладку полетников с экрана: только панель папки + CrossPlateTestingPanel.</summary>
        private void ApplyCrossOnlyStandaloneLayout(Panel folderBar)
        {
            SuspendLayout();
            try
            {
                tabCrossPlate.Controls.Remove(pnlCrossHost);
                Controls.Remove(tabMain);

                folderBar.Dock = DockStyle.Top;
                pnlCrossHost.Dock = DockStyle.Fill;

                Controls.Add(pnlCrossHost);
                Controls.Add(folderBar);

                tabMain.Visible = false;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void TabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_crossOnlyMode) return;
            if (tabMain.SelectedTab == tabCrossPlate)
                _crossPanel?.BeginInvoke(new Action(() => _crossPanel.RelayoutEmbeddedUi()));
        }

        private void AutoTestingForm_Load(object sender, EventArgs e)
        {
            if (_crossOnlyMode)
            {
                _crossPanel?.BeginInvoke(new Action(() => _crossPanel.RelayoutEmbeddedUi()));
                return;
            }

            var state = StandsStateHelper.LoadState(StandsStateHelper.PoletnikiStateFileName);
            if (state != null && state.Count > 0)
            {
                foreach (var s in state)
                {
                    var row = new AutoTestRowControl();
                    row.SetLoadedState(s.StandNumber, s.VolumeSerialNumber, s.SerialNumber, s.VolumeLabel);
                    SubscribeRowEvents(row);
                    row.SetAutoCompleteSource(_productsForAutocomplete);
                    pnlRows.Controls.Add(row);
                }
            }
        }

        private void AutoTestingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_crossOnlyMode)
                SaveStandsState();
        }

        private void SubscribeRowEvents(AutoTestRowControl row)
        {
            row.RemoveRequested += Row_RemoveRequested;
            row.ErrorRequested += Row_ErrorRequested;
            row.FlashSaved += Row_FlashSaved;
            row.SerialNumberChanged += Row_SerialNumberChanged;
        }

        private void Row_FlashSaved(object sender, AutoTestRowControl.FlashSavedEventArgs e)
        {
            AppendLog($"[Сохранено] С/Н: {e.SerialNumber}, флешка: {e.VolumeSerialNumber}, стенд №{e.StandNumber}");
            SaveStandsState();
        }

        private void Row_SerialNumberChanged(object sender, AutoTestRowControl.SerialNumberChangedEventArgs e)
        {
            AppendLog($"[Изменён С/Н] Стенд №{e.StandNumber}: {e.OldSerial} → {e.NewSerial}");
            SaveStandsState();
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendLogCore), text);
                return;
            }
            AppendLogCore(text);
        }

        private void AppendLogCore(string text)
        {
            string stamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText($"[{stamp}] {text}\r\n");
        }

        private void SaveStandsState()
        {
            var rows = pnlRows.Controls.OfType<AutoTestRowControl>().ToList();
            var state = rows.Select(r => new StandsStateHelper.StandState
            {
                StandNumber = r.Stand,
                VolumeSerialNumber = r.SavedVolumeSerialNumber,
                VolumeLabel = r.SavedVolumeLabel,
                SerialNumber = r.SerialNumber
            }).ToList();
            StandsStateHelper.SaveState(state, StandsStateHelper.PoletnikiStateFileName);
        }

        private void LoadSettings()
        {
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    string path = ctx.GetSavePathForAct("AutoTesting");
                    if (!string.IsNullOrEmpty(path))
                    {
                        txtFolderPath.Text = path;
                        if (_txtFolderCrossRoot != null)
                            _txtFolderCrossRoot.Text = path;
                    }
                }
            }
            catch { }
        }

        private void SaveSettings(string path)
        {
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    ctx.SetSavePathForAct("AutoTesting", path);
                }
            }
            catch { }
        }

        private string GetEffectiveRootPath()
        {
            string basePath = txtFolderPath.Text.Trim();
            if (string.IsNullOrEmpty(basePath)) return null;
            return Path.Combine(basePath, $"Отгрузка_{_productTypeName}_Акт_{_actNumber}");
        }

        private void LoadProductsForAutocomplete()
        {
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    _productsForAutocomplete = ctx.Product.AsNoTracking()
                        .Where(p => p.ActID == _actId && p.ProducType != null && p.ProducType.TypeName == _productTypeName)
                        .Select(p => p.ProductSerial)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    RefreshAutocompleteInRows();
                }
            }
            catch { }
        }

        private void RefreshAutocompleteInRows()
        {
            foreach (var row in pnlRows.Controls.OfType<AutoTestRowControl>())
                row.SetAutoCompleteSource(_productsForAutocomplete);
        }

        private void LoadProductsWithErrors()
        {
            _productsWithErrorsList.Clear();
            lstProductsWithErrors.Items.Clear();
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    var products = ctx.Product.AsNoTracking()
                        .Where(p => p.ActID == _actId && p.ProducType != null && p.ProducType.TypeName == _productTypeName)
                        .ToDictionary(p => p.ProductID, p => p.ProductSerial ?? "");
                    var tflights = ctx.TechnicalMatFlight.AsNoTracking()
                        .Where(t => t.Test_Pass == false && products.ContainsKey(t.ProductID))
                        .Select(tf => new { tf.TFlightID, tf.ProductID })
                        .ToList();

                    foreach (var item in tflights)
                    {
                        var testFlights = ctx.TestFlight.AsNoTracking()
                            .Where(t => t.TFlightID == item.TFlightID)
                            .ToList();
                        if (testFlights.Count < 3) continue;
                        int emptyCount = testFlights.Count(t => string.IsNullOrWhiteSpace(t.Result));
                        if (emptyCount >= 2)
                        {
                            string serial = products.TryGetValue(item.ProductID, out var s) ? s : "";
                            _productsWithErrorsList.Add(new ProductsWithErrorsItem
                            {
                                ProductID = item.ProductID,
                                TFlightID = item.TFlightID,
                                SerialNumber = serial
                            });
                            lstProductsWithErrors.Items.Add(serial);
                        }
                    }
                }
            }
            catch { }
        }

        private class ProductsWithErrorsItem
        {
            public int ProductID { get; set; }
            public int TFlightID { get; set; }
            public string SerialNumber { get; set; }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var target = _txtFolderCrossRoot ?? txtFolderPath;
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите корневую папку для тестирования";
                if (!string.IsNullOrEmpty(target.Text) && Directory.Exists(target.Text))
                    fbd.SelectedPath = target.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                    if (_txtFolderCrossRoot != null)
                        _txtFolderCrossRoot.Text = fbd.SelectedPath;
                    SaveSettings(fbd.SelectedPath);
                }
            }
        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            var row = new AutoTestRowControl();
            SubscribeRowEvents(row);
            row.SetAutoCompleteSource(_productsForAutocomplete);
            pnlRows.Controls.Add(row);
        }

        private void Row_RemoveRequested(object sender, EventArgs e)
        {
            if (sender is AutoTestRowControl row)
            {
                row.RemoveRequested -= Row_RemoveRequested;
                row.ErrorRequested -= Row_ErrorRequested;
                row.FlashSaved -= Row_FlashSaved;
                row.SerialNumberChanged -= Row_SerialNumberChanged;
                pnlRows.Controls.Remove(row);
                row.Dispose();
                SaveStandsState();
            }
        }

        private void Row_ErrorRequested(object sender, EventArgs e)
        {
            if (sender is AutoTestRowControl row && !string.IsNullOrEmpty(row.SerialNumber))
                OpenErrorFormForProduct(row.SerialNumber, null);
        }

        private void OpenErrorFormForProduct(string serialNumber, List<TestFlight> existingRecords)
        {
            int productId = 0;
            int tflightId = 0;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                var product = ctx.Product.AsNoTracking()
                    .FirstOrDefault(p => p.ActID == _actId && p.ProductSerial == serialNumber);
                if (product == null)
                {
                    MessageBox.Show("Продукт не найден.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                productId = product.ProductID;

                var tflight = ctx.TechnicalMatFlight.AsNoTracking()
                    .Where(t => t.ProductID == productId && !t.Test_Pass)
                    .OrderByDescending(t => t.Date)
                    .FirstOrDefault();
                if (tflight == null)
                {
                    tflight = new TechnicalMatFlight
                    {
                        Date = DateTime.Now,
                        ProductID = productId,
                        UserID = _user.UserID,
                        Test_Pass = false
                    };
                    ctx.TechnicalMatFlight.Add(tflight);
                    ctx.SaveChanges();
                    tflightId = tflight.TFlightID;
                }
                else
                {
                    tflightId = tflight.TFlightID;
                    if (existingRecords == null)
                    {
                        existingRecords = ctx.TestFlight.AsNoTracking()
                            .Where(t => t.TFlightID == tflightId)
                            .OrderBy(t => t.TestID)
                            .ToList();
                    }
                }
            }

            var form = new FlightTestErrorForm(productId, tflightId, serialNumber, _actNumber, _user, () =>
            {
                LoadProductsWithErrors();
                LoadProductsForAutocomplete();
                NotifyEmployeeProductsRefresh();
            }, existingRecords);
            form.Show(this);
        }

        private void lstProductsWithErrors_DoubleClick(object sender, EventArgs e)
        {
            int idx = lstProductsWithErrors.SelectedIndex;
            if (idx < 0 || idx >= _productsWithErrorsList.Count) return;

            var item = _productsWithErrorsList[idx];
            List<TestFlight> existing = null;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                existing = ctx.TestFlight.AsNoTracking()
                    .Where(t => t.TFlightID == item.TFlightID)
                    .OrderBy(t => t.TestID)
                    .ToList();
            }
            OpenErrorFormForProduct(item.SerialNumber, existing);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string basePath = txtFolderPath.Text.Trim();
            if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
            {
                MessageBox.Show("Пожалуйста, выберите существующую папку для сохранения.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rows = pnlRows.Controls.OfType<AutoTestRowControl>().ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один стенд/строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.SavedVolumeSerialNumber))
                {
                    MessageBox.Show("На всех добавленных строках необходимо выбрать флешку и нажать 'Сохранить'.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(row.SerialNumber))
                {
                    MessageBox.Show("Пожалуйста, укажите серийный номер продукта во всех строках.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _isStarted = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnAddRow.Enabled = false;
            btnBrowse.Enabled = false;
            txtFolderPath.Enabled = false;

            foreach (var r in rows)
                r.SetMonitoringMode(true);

            lblMainStatus.Text = "Статус: Мониторинг запущен";
            lblMainStatus.ForeColor = Color.Green;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _isStarted = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            btnAddRow.Enabled = true;
            btnBrowse.Enabled = true;
            txtFolderPath.Enabled = true;

            foreach (var r in pnlRows.Controls.OfType<AutoTestRowControl>())
                r.SetMonitoringMode(false);

            lblMainStatus.Text = "Статус: Остановлено";
            lblMainStatus.ForeColor = Color.Red;
        }

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_isStarted && m.Msg == WM_DEVICECHANGE)
            {
                if (m.WParam.ToInt32() == DBT_DEVICEARRIVAL)
                {
                    Task.Delay(2000).ContinueWith(_ => CheckForDrives());
                }
            }
        }

        private void CheckForDrives()
        {
            if (!_isStarted) return;

            DriveInfo[] drives = UsbHelper.GetRemovableDrives();

            if (InvokeRequired)
            {
                Invoke(new Action(() => CheckForDrivesCore(drives)));
            }
            else
            {
                CheckForDrivesCore(drives);
            }
        }

        private void CheckForDrivesCore(DriveInfo[] drives)
        {
            var rows = pnlRows.Controls.OfType<AutoTestRowControl>().ToList();
            string rootPath = GetEffectiveRootPath();
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = txtFolderPath.Text;
                if (!string.IsNullOrEmpty(rootPath))
                    rootPath = Path.Combine(rootPath, $"Отгрузка_{_productTypeName}_Акт_{_actNumber}");
            }
            if (string.IsNullOrEmpty(rootPath)) return;

            foreach (var drive in drives)
            {
                string volSerial = UsbHelper.GetVolumeSerialNumber(drive.Name);
                if (string.IsNullOrEmpty(volSerial)) continue;

                var matchedRow = rows.FirstOrDefault(r => r.SavedVolumeSerialNumber == volSerial);
                if (matchedRow != null)
                {
                    ProcessDriveAsync(drive.Name, volSerial, matchedRow, rootPath);
                }
            }
        }

        private async void ProcessDriveAsync(string driveLetter, string volSerial, AutoTestRowControl row, string rootPath)
        {
            string productSerial = row.SerialNumber;
            string standNumber = row.Stand;
            string targetFolder = Path.Combine(rootPath, productSerial);

            bool hasData = await Task.Run(() => UsbHelper.HasFilesWithExtensions(driveLetter, new[] { ".txt", ".log" }));
            if (!hasData)
            {
                AppendLog($"[Флешка пуста] С/Н: {productSerial}, флешка: {volSerial}, стенд №{standNumber}");
                Invoke(new Action(() => MessageBox.Show("На флешке нет данных для сохранения", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                row.SetStatus("Флешка пуста", Color.Orange);
                return;
            }

            row.SetStatus("Копирование...", Color.Orange);

            Directory.CreateDirectory(rootPath);
            string tempFolder = Path.Combine(rootPath, "_temp_" + Guid.NewGuid().ToString("N"));
            string copyErrorMsg = null;
            bool copySuccess = await Task.Run(() =>
            {
                try
                {
                    UsbHelper.CopyDirectoryWithExtensions(driveLetter, tempFolder, new[] { ".txt", ".log" });
                    UsbHelper.RemoveSubfolderIfExists(tempFolder, "System Volume Information");
                    UsbHelper.ClearDirectoryWithExtensions(driveLetter, new[] { ".txt", ".log" });
                    return true;
                }
                catch (Exception ex)
                {
                    copyErrorMsg = ex.Message;
                    Invoke(new Action(() => MessageBox.Show($"Ошибка копирования для С/Н {productSerial}: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    return false;
                }
            });

            if (!copySuccess)
            {
                AppendLog($"[Ошибка копирования] С/Н: {productSerial}, флешка: {volSerial}, стенд №{standNumber}: {copyErrorMsg ?? "неизвестная ошибка"}");
                row.SetStatus("Ошибка копирования", Color.Red);
                if (Directory.Exists(tempFolder))
                    try { Directory.Delete(tempFolder, true); } catch { }
                return;
            }

            bool testPassed = ExcelReportHelper.FolderHasSuccess(tempFolder);
            string reportPath = ExcelReportHelper.GetReportPath(rootPath, _actNumber);

            if (testPassed)
            {
                bool folderExists = Directory.Exists(targetFolder);
                bool serialInExcel = ExcelReportHelper.SerialExistsInReport(reportPath, productSerial);
                if (folderExists || serialInExcel)
                {
                    string newName = null;
                    Invoke(new Action(() =>
                    {
                        string msg = folderExists && serialInExcel
                            ? "Папка и запись в Excel с таким С/Н уже существуют."
                            : folderExists
                                ? "Папка с таким С/Н уже существует."
                                : "Запись с таким С/Н уже есть в Excel.";
                        newName = InputDialogHelper.Show(this, "Конфликт", msg + " Введите новое название С/Н:", productSerial);
                    }));
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        row.SetStatus("Отменено (конфликт)", Color.Orange);
                        try { Directory.Delete(tempFolder, true); } catch { }
                        return;
                    }
                    productSerial = newName.Trim();
                    targetFolder = Path.Combine(rootPath, productSerial);
                }
            }

            try
            {
                if (Directory.Exists(targetFolder))
                    Directory.Delete(targetFolder, true);
                Directory.Move(tempFolder, targetFolder);
            }
            catch (Exception ex)
            {
                try { Directory.Delete(tempFolder, true); } catch { }
                Invoke(new Action(() => MessageBox.Show($"Ошибка перемещения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                row.SetStatus("Ошибка", Color.Red);
                return;
            }

            string performer = _user?.UserName ?? "";
            bool dbWritten = false;

            using (var ctx = ConnectionHelper.CreateContext())
            {
                var product = ctx.Product.AsNoTracking().FirstOrDefault(p => p.ActID == _actId && p.ProductSerial == productSerial);
                if (product == null)
                {
                    Invoke(new Action(() => MessageBox.Show("Продукт не найден в БД.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    row.SetStatus("Ошибка: продукт не найден", Color.Red);
                    return;
                }

                var tflight = new TechnicalMatFlight
                {
                    Date = DateTime.Now,
                    ProductID = product.ProductID,
                    UserID = _user.UserID,
                    Test_Pass = testPassed
                };
                ctx.TechnicalMatFlight.Add(tflight);
                ctx.SaveChanges();
                int tflightId = tflight.TFlightID;

                try
                {
                    if (testPassed)
                    {
                        int nextRowNum = ExcelReportHelper.GetNextRowNumber(reportPath, ExcelReportHelper.SheetActsName);
                        ExcelReportHelper.AddActEntry(reportPath, _actNumber, performer, productSerial, standNumber, nextRowNum);
                        ctx.TestFlight.Add(new TestFlight
                        {
                            TFlightID = tflightId,
                            Stand = int.TryParse(standNumber, out int s) ? s : 0,
                            Visual = true, Damage = true, FC1 = true, FC2 = true, C_5V_FC1 = true, C_5V_FC2 = true,
                            FC_Test_Pass = true, Externa_Test_Pass = true, Long_Test_Pass = true,
                            Result = "", Description = ""
                        });
                        ctx.SaveChanges();
                    }
                    else
                    {
                        int nextRowNum = ExcelReportHelper.GetNextRowNumber(reportPath, ExcelReportHelper.SheetRepairName);
                        ExcelReportHelper.AddRepairEntry(reportPath, _actNumber, performer, productSerial, standNumber, nextRowNum);
                        var errorForm = new FlightTestErrorForm(product.ProductID, tflightId, productSerial, _actNumber, _user, () =>
                        {
                            LoadProductsWithErrors();
                            LoadProductsForAutocomplete();
                            NotifyEmployeeProductsRefresh();
                        }, null);
                        Invoke(new Action(() =>
                        {
                            MessageBox.Show("Тест пройден с ошибкой", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            errorForm.Show(this);
                            string txtPath = ExcelReportHelper.GetFirstTxtFile(targetFolder);
                            if (!string.IsNullOrEmpty(txtPath))
                                ExcelReportHelper.OpenFile(txtPath);
                            ExcelReportHelper.OpenFile(reportPath);
                        }));
                    }
                    dbWritten = true;
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() => MessageBox.Show($"Ошибка записи в отчёт: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    row.SetStatus("Ошибка записи отчёта", Color.Red);
                    return;
                }
            }

            if (dbWritten)
                AppendLog($"[Лог сохранён] С/Н: {productSerial}, флешка: {volSerial}, стенд №{standNumber}");

            row.SetStatus("Извлечение...", Color.Orange);
            bool ejected = UsbHelper.EjectDrive(driveLetter[0]);

            if (testPassed)
            {
                if (ejected)
                    row.SetStatus("Успех: скопировано и извлечено", Color.Green);
                else
                    row.SetStatus("Скопировано, но не извлечено", Color.Goldenrod);
            }
            else
            {
                if (ejected)
                    row.SetStatus("Тест с ошибкой: скопировано и извлечено", Color.Goldenrod);
                else
                    row.SetStatus("Тест с ошибкой: скопировано", Color.Goldenrod);
            }

            Invoke(new Action(() =>
            {
                LoadProductsWithErrors();
                LoadProductsForAutocomplete();
                NotifyEmployeeProductsRefresh();
            }));
        }
    }
}
