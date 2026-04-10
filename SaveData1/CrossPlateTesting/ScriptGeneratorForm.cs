using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SaveData1.CrossPlateTesting.Controls;
using SaveData1.CrossPlateTesting.Models;
using SaveData1.CrossPlateTesting.Services;

namespace SaveData1.CrossPlateTesting
{
    /// <summary>
    /// Генератор скриптов .mavparams — режим кода и визуальный, синхронизация в обе стороны.
    /// </summary>
    public partial class ScriptGeneratorForm : Form
    {
        private SplitContainer splitMain;
        private TextBox txtScript;
        private ScriptCanvasControl canvas;
        private ComboBox cmbParam;
        private Button btnAddSet, btnAddIf, btnAddRead;
        private Button btnSave, btnOpen, btnTest;
        private Button btnViewCode, btnViewVisual, btnViewSplit;
        private TextBox txtLog;
        private string[] _paramNames;
        private AppConfig _config;
        private bool _codeChangeInProgress;
        private bool _modelChangeInProgress;
        private System.Windows.Forms.Timer _syncTimer;

        public ScriptGeneratorForm()
        {
            InitializeComponent();
            SetupUI();
            LoadParameterReference();
            _config = new AppConfig();
        }

        private void SetupUI()
        {
            Text = "Генератор скриптов MAVLink";
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            BackColor = Color.FromArgb(45, 45, 50);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 8, 8, 8),
                RowCount = 4,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

            // Toolbar
            var toolbar = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 36, Dock = DockStyle.Fill };
            toolbar.Controls.Add(new Label { Text = "Параметр:", AutoSize = true, ForeColor = Color.White, Margin = new Padding(0, 8, 4, 0) });
            cmbParam = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                BackColor = Color.FromArgb(55, 55, 60),
                ForeColor = Color.White
            };
            toolbar.Controls.Add(cmbParam);
            btnAddSet = new Button { Text = "Set", Width = 60, Margin = new Padding(6, 4, 2, 4), BackColor = Color.FromArgb(60, 100, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddIf = new Button { Text = "If", Width = 60, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(70, 90, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddRead = new Button { Text = "Read", Width = 65, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(70, 80, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnAddWhile = new Button { Text = "While", Width = 65, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(90, 70, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnAddVar = new Button { Text = "Var", Width = 60, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(100, 80, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnAddSleep = new Button { Text = "Sleep", Width = 65, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(80, 70, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnAddMode = new Button { Text = "Mode", Width = 65, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(100, 70, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnAutoLayout = new Button { Text = "Auto Layout", Width = 95, Margin = new Padding(15, 4, 2, 4), BackColor = Color.FromArgb(70, 90, 110), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnClear = new Button { Text = "🗑", Width = 40, Margin = new Padding(10, 4, 2, 4), BackColor = Color.FromArgb(120, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            
            toolbar.Controls.Add(btnAddSet);
            toolbar.Controls.Add(btnAddIf);
            toolbar.Controls.Add(btnAddRead);
            toolbar.Controls.Add(btnAddWhile);
            toolbar.Controls.Add(btnAddVar);
            toolbar.Controls.Add(btnAddSleep);
            toolbar.Controls.Add(btnAddMode);
            toolbar.Controls.Add(btnAutoLayout);
            toolbar.Controls.Add(btnClear);
            toolbar.Controls.Add(new Panel { Width = 20 });
            btnViewCode = new Button { Text = "Код", Width = 60, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(60, 60, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnViewVisual = new Button { Text = "Визуал", Width = 70, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(60, 60, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnViewSplit = new Button { Text = "Оба", Width = 60, Margin = new Padding(2, 4, 2, 4), BackColor = Color.FromArgb(80, 100, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            toolbar.Controls.Add(btnViewCode);
            toolbar.Controls.Add(btnViewVisual);
            toolbar.Controls.Add(btnViewSplit);
            mainPanel.Controls.Add(toolbar, 0, 0);

            // Buttons row
            var btnRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 32, Dock = DockStyle.Fill };
            btnSave = new Button { Text = "Сохранить", Width = 95, Margin = new Padding(0, 2, 4, 2), BackColor = Color.FromArgb(70, 90, 110), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnOpen = new Button { Text = "Открыть", Width = 95, Margin = new Padding(2, 2, 2, 2), BackColor = Color.FromArgb(70, 90, 110), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnTest = new Button { Text = "Тест", Width = 95, Margin = new Padding(2, 2, 2, 2), BackColor = Color.FromArgb(80, 100, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRow.Controls.Add(btnSave);
            btnRow.Controls.Add(btnOpen);
            btnRow.Controls.Add(btnTest);
            mainPanel.Controls.Add(btnRow, 0, 1);

            // Split: Code | Visual
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 400,
                BackColor = Color.FromArgb(40, 40, 45),
                SplitterWidth = 6
            };

            txtScript = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsTab = true,
                BackColor = Color.FromArgb(35, 35, 40),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None
            };
            txtScript.Text = @"# Скрипт параметров MAVLink (.mavparams)
# var NAME=value | NAME=expr | NAME++ | if/while VAR op value

";
            var codePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 4) };
            codePanel.Controls.Add(txtScript);
            var codeGroup = new GroupBox
            {
                Text = "  Код  ",
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 4, 4, 4),
                ForeColor = Color.FromArgb(180, 190, 200),
                BackColor = Color.FromArgb(40, 40, 45)
            };
            codeGroup.Controls.Add(txtScript);
            splitMain.Panel1.Controls.Add(codeGroup);

            canvas = new ScriptCanvasControl { Dock = DockStyle.Fill };
            var visualGroup = new GroupBox
            {
                Text = "  Визуальный редактор (ноды, связи)  ",
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 4, 4, 4),
                ForeColor = Color.FromArgb(180, 190, 200),
                BackColor = Color.FromArgb(40, 40, 45)
            };
            visualGroup.Controls.Add(canvas);
            splitMain.Panel2.Controls.Add(visualGroup);
            canvas.ModelChanged += (root) => SyncVisualToCode(root);
            canvas.DefaultParamName = GetSelectedParam();
            cmbParam.TextChanged += (s, ev) => canvas.DefaultParamName = GetSelectedParam();

            mainPanel.Controls.Add(splitMain, 0, 2);

            // Log
            txtLog = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9f),
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 35),
                ForeColor = Color.FromArgb(200, 200, 200),
                BorderStyle = BorderStyle.None
            };
            var logGroup = new GroupBox
            {
                Text = "  Лог теста  ",
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 4, 4, 4),
                ForeColor = Color.FromArgb(180, 190, 200),
                BackColor = Color.FromArgb(40, 40, 45)
            };
            logGroup.Controls.Add(txtLog);
            mainPanel.Controls.Add(logGroup, 0, 3);

            Controls.Add(mainPanel);

            _syncTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _syncTimer.Tick += (s, ev) => { _syncTimer.Stop(); SyncCodeToVisual(); };
            txtScript.TextChanged += TxtScript_TextChanged;
            btnAddSet.Click += BtnAddSet_Click;
            btnAddIf.Click += BtnAddIf_Click;
            btnAddRead.Click += BtnAddRead_Click;
            btnAddWhile.Click += (s, ev) => { string p = GetSelectedParam(); InsertAtCursor($"while {p} == 0{Environment.NewLine}  set {p} 1{Environment.NewLine}endwhile{Environment.NewLine}"); };
            btnAddVar.Click += (s, ev) => InsertAtCursor("var cycle = 1" + Environment.NewLine + "while cycle < 5" + Environment.NewLine + "  set " + GetSelectedParam() + " 0" + Environment.NewLine + "  cycle++" + Environment.NewLine + "endwhile" + Environment.NewLine);
            btnAddSleep.Click += (s, ev) => InsertAtCursor("sleep 1" + Environment.NewLine);
            btnAddMode.Click += (s, ev) => InsertAtCursor("mode LOITER" + Environment.NewLine);
            btnAutoLayout.Click += (s, ev) => 
            { 
                var root = canvas.Root;
                if (root != null)
                {
                    if (root.UseConnectionGraph && (root.Connections?.Count ?? 0) > 0)
                    {
                        ScriptNodeTree.BuildTreeFromConnections(root);
                    }
                    root.UseConnectionGraph = false;
                    root.Connections?.Clear();
                    SyncVisualToCode(root);
                }
            };
            btnClear.Click += (s, ev) => { if (MessageBox.Show("Очистить весь скрипт?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) txtScript.Text = ""; };
            btnSave.Click += BtnSave_Click;
            btnOpen.Click += BtnOpen_Click;
            btnTest.Click += BtnTest_Click;
            btnViewCode.Click += (s, e) => SetViewMode(0);
            btnViewVisual.Click += (s, e) => SetViewMode(1);
            btnViewSplit.Click += (s, e) => SetViewMode(2);

            SyncCodeToVisual();
        }

        private void SetViewMode(int mode)
        {
            splitMain.Panel1Collapsed = (mode == 1);
            splitMain.Panel2Collapsed = (mode == 0);
            if (mode == 2)
            {
                splitMain.Panel1Collapsed = false;
                splitMain.Panel2Collapsed = false;
            }
            btnViewCode.BackColor = mode == 0 ? Color.FromArgb(80, 100, 130) : Color.FromArgb(60, 60, 70);
            btnViewVisual.BackColor = mode == 1 ? Color.FromArgb(80, 100, 130) : Color.FromArgb(60, 60, 70);
            btnViewSplit.BackColor = mode == 2 ? Color.FromArgb(80, 100, 130) : Color.FromArgb(60, 60, 70);
        }

        private void TxtScript_TextChanged(object sender, EventArgs e)
        {
            if (_modelChangeInProgress) return;
            _syncTimer.Stop();
            _syncTimer.Start();
        }

        private void SyncCodeToVisual()
        {
            if (_modelChangeInProgress) return;
            _codeChangeInProgress = true;
            try
            {
                var root = ScriptNodeTree.Parse(txtScript.Lines ?? new string[0]);
                canvas.Root = root;
            }
            finally
            {
                _codeChangeInProgress = false;
            }
        }

        private void SyncVisualToCode(RootNode root)
        {
            if (_codeChangeInProgress) return;
            if (InvokeRequired) { BeginInvoke(new Action<RootNode>(SyncVisualToCode), root); return; }
            _modelChangeInProgress = true;
            try
            {
                if (root.UseConnectionGraph && (root.Connections?.Count ?? 0) > 0)
                {
                    ScriptNodeTree.BuildTreeFromConnections(root);
                }
                var code = ScriptNodeTree.ToCode(root);
                txtScript.Text = code;
            }
            finally
            {
                _modelChangeInProgress = false;
            }
        }

        private void LoadParameterReference()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parameters", "ArduCopterCommon.json");
                if (!File.Exists(path))
                {
                    _paramNames = new[] { "SERVO1_REVERSED", "SERVO1_TRIM", "SERVO2_REVERSED", "SERVO3_TRIM", "SERVO4_TRIM" };
                }
                else
                {
                    string json = File.ReadAllText(path);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var data = serializer.Deserialize<ParameterReferenceFile>(json);
                    _paramNames = data?.parameters?.Select(p => p.name).ToArray() ?? new string[0];
                }
                cmbParam.Items.Clear();
                foreach (var n in _paramNames ?? new string[0])
                    cmbParam.Items.Add(n);
            }
            catch
            {
                _paramNames = new[] { "SERVO1_REVERSED", "SERVO1_TRIM", "SERVO2_REVERSED", "SERVO3_TRIM", "SERVO4_TRIM" };
                cmbParam.Items.Clear();
                foreach (var n in _paramNames)
                    cmbParam.Items.Add(n);
            }
        }

        private string GetSelectedParam()
        {
            string p = cmbParam.Text?.Trim();
            if (string.IsNullOrEmpty(p) && cmbParam.Items.Count > 0)
                p = cmbParam.Items[0]?.ToString();
            return p ?? "SERVO1_REVERSED";
        }

        private void InsertAtCursor(string text)
        {
            int pos = txtScript.SelectionStart;
            string content = txtScript.Text;
            txtScript.Text = content.Insert(pos, text);
            txtScript.SelectionStart = pos + text.Length;
            txtScript.Focus();
        }

        private void BtnAddSet_Click(object sender, EventArgs e)
        {
            string param = GetSelectedParam();
            InsertAtCursor($"set {param} 0{Environment.NewLine}");
        }

        private void BtnAddIf_Click(object sender, EventArgs e)
        {
            string param = GetSelectedParam();
            InsertAtCursor($"if {param} == 0{Environment.NewLine}  set {param} 1{Environment.NewLine}else{Environment.NewLine}  set {param} 0{Environment.NewLine}endif{Environment.NewLine}");
        }

        private void BtnAddRead_Click(object sender, EventArgs e)
        {
            string param = GetSelectedParam();
            InsertAtCursor($"read {param}{Environment.NewLine}");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Скрипты параметров (*.mavparams)|*.mavparams|Все файлы (*.*)|*.*";
                dlg.DefaultExt = "mavparams";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlg.FileName, txtScript.Text);
                    LogMsg($"Сохранено: {dlg.FileName}");
                }
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Скрипты параметров (*.mavparams)|*.mavparams|Все файлы (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtScript.Text = File.ReadAllText(dlg.FileName);
                    LogMsg($"Открыто: {dlg.FileName}");
                }
            }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            btnTest.Enabled = false;
            txtLog.Clear();
            try
            {
                var root = ScriptNodeTree.Parse(txtScript.Lines ?? new string[0]);
                if (root.Children.Count == 0)
                {
                    LogMsg("[ОШИБКА] Скрипт пуст или не распознан.");
                    return;
                }

                string host = "192.168.4.1";
                int port = _config.DronePort > 0 ? _config.DronePort : 14550;
                if (!string.IsNullOrWhiteSpace(_config.DronePingAddress))
                {
                    var addrs = _config.DronePingAddress.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (addrs.Length > 0) host = addrs[0].Trim();
                }

                LogMsg($"[MAVLink] Хост: {host}, порт: {port}");
                LogMsg("[MAVLink] Выполнение...");
                await ScriptNodeTreeExecutor.RunAsync(root, host, port, LogMsg);
                LogMsg("[MAVLink] Тест завершён.");
            }
            catch (Exception ex)
            {
                LogMsg($"[ОШИБКА] {ex.Message}");
            }
            finally
            {
                btnTest.Enabled = true;
            }
        }

        private void LogMsg(string msg)
        {
            if (IsDisposed || txtLog == null || txtLog.IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(LogMsg), msg);
                return;
            }
            if (txtLog.IsDisposed) return;
            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.ScrollToCaret();
        }
    }
}
