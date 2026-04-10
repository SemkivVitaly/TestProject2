using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SaveData1;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Froms
{
    /// <summary>Сохранение логов ESP32 Bridge: серийный номер продукта типа Bridge из акта (см. BridgeDbHelper / App.config).</summary>
    public partial class BridgeLogForm : Form
    {
        private const int EmSetCueBanner = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private readonly UsersProfile _user;
        private readonly int _actId;
        private readonly string _actNumber;
        private readonly string _employeeFio;

        private List<string> _allowedSerials = new List<string>();
        private bool _updatingSerialFromAutocomplete;

        public BridgeLogForm(UsersProfile user, int actId, string actNumber)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(actNumber)) throw new ArgumentException("Номер акта не задан.", nameof(actNumber));

            InitializeComponent();
            _user = user;
            _actId = actId;
            _actNumber = actNumber.Trim();
            _employeeFio = (user.UserName ?? "").Trim();

            lblAct.Text = "Акт № " + _actNumber;
            lblEmployee.Text = "Исполнитель: " + (string.IsNullOrEmpty(_employeeFio) ? "—" : _employeeFio);
        }

        private void BridgeLogForm_Load(object sender, EventArgs e)
        {
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    string root = ctx.GetSavePathForAct(_actNumber);
                    if (!string.IsNullOrWhiteSpace(root))
                        txtReportsPath.Text = root.Trim();
                }
            }
            catch
            {
                /* пользователь укажет путь вручную */
            }

            try
            {
                _allowedSerials = LoadBridgeProductSerialsForAct();
            }
            catch
            {
                _allowedSerials = new List<string>();
            }

            SetSerialAutocompleteSource(_allowedSerials);
            txtSerial.TextChanged += TxtSerial_AutoCompleteNormalize_TextChanged;

            if (_allowedSerials.Count == 0)
            {
                labelQueue.Text = "В акте нет продуктов типа «" + BridgeDbHelper.GetBridgeProductTypeName() +
                    "» с заполненным серийным номером — сохранение логов недоступно.";
                txtSerial.Enabled = false;
                btnSaveLogs.Enabled = false;
            }
            else
            {
                void SetCue()
                {
                    if (!txtSerial.IsHandleCreated) return;
                    SendMessage(txtSerial.Handle, EmSetCueBanner, (IntPtr)0,
                        "Начните ввод или выберите из подсказок (Bridge этого акта)…");
                }
                if (txtSerial.IsHandleCreated)
                    SetCue();
                else
                    txtSerial.HandleCreated += (_, __) => SetCue();
            }
        }

        /// <summary>Серийные номера продуктов типа Bridge в текущем акте.</summary>
        private List<string> LoadBridgeProductSerialsForAct()
        {
            if (_actId <= 0) return new List<string>();
            string typeName = BridgeDbHelper.GetBridgeProductTypeName();
            using (var ctx = ConnectionHelper.CreateContext())
            {
                return ctx.Product.AsNoTracking()
                    .Where(p => p.ActID == _actId && p.ProducType != null &&
                        p.ProducType.TypeName == typeName)
                    .Select(p => p.ProductSerial)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();
            }
        }

        private void SetSerialAutocompleteSource(IEnumerable<string> serials)
        {
            _allowedSerials = serials?.Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList() ?? new List<string>();
            var src = new AutoCompleteStringCollection();
            foreach (var s in _allowedSerials)
                src.Add(s);
            var added = new HashSet<string>();
            foreach (var s in _allowedSerials)
            {
                string last4 = GetLast4DigitsForAutocomplete(s);
                if (!string.IsNullOrEmpty(last4))
                {
                    string key = last4 + " - " + s;
                    if (added.Add(key))
                        src.Add(key);
                }
            }
            txtSerial.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtSerial.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtSerial.AutoCompleteCustomSource = src;
        }

        private static string GetLast4DigitsForAutocomplete(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 4) return null;
            var digits = new string(s.Reverse().TakeWhile(char.IsDigit).Take(4).Reverse().ToArray());
            return digits.Length == 4 ? digits : null;
        }

        private void TxtSerial_AutoCompleteNormalize_TextChanged(object sender, EventArgs e)
        {
            if (_updatingSerialFromAutocomplete) return;
            string t = txtSerial.Text;
            if (string.IsNullOrEmpty(t) || !t.Contains(" - ")) return;
            int idx = t.LastIndexOf(" - ", StringComparison.Ordinal);
            string afterDash = t.Substring(idx + 3).Trim();
            string canonical = _allowedSerials.FirstOrDefault(s => string.Equals(s, afterDash, StringComparison.OrdinalIgnoreCase));
            if (canonical != null)
            {
                _updatingSerialFromAutocomplete = true;
                txtSerial.Text = canonical;
                txtSerial.SelectAll();
                _updatingSerialFromAutocomplete = false;
            }
        }

        private string GetEnteredSerialRaw()
        {
            return txtSerial.Text?.Trim() ?? "";
        }

        private bool TryResolveCanonicalSerial(string input, out string canonical)
        {
            canonical = null;
            if (string.IsNullOrEmpty(input)) return false;
            if (input.Contains(" - "))
            {
                int idx = input.LastIndexOf(" - ", StringComparison.Ordinal);
                string afterDash = input.Substring(idx + 3).Trim();
                canonical = _allowedSerials.FirstOrDefault(s => string.Equals(s, afterDash, StringComparison.OrdinalIgnoreCase));
                if (canonical != null) return true;
            }
            canonical = _allowedSerials.FirstOrDefault(s => string.Equals(s, input, StringComparison.OrdinalIgnoreCase));
            return canonical != null;
        }

        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Корневая папка: внутри будет создана папка с именем по № акта";
                dlg.SelectedPath = string.IsNullOrWhiteSpace(txtReportsPath.Text)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    : txtReportsPath.Text;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtReportsPath.Text = dlg.SelectedPath;
            }
        }

        private async void btnSaveLogs_Click(object sender, EventArgs e)
        {
            string fio = _employeeFio;
            string act = _actNumber;
            string root = txtReportsPath.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(fio))
            {
                MessageBox.Show(this, "У текущего пользователя не указано имя (ФИО) в профиле.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!TryResolveCanonicalSerial(GetEnteredSerialRaw(), out string serial))
            {
                MessageBox.Show(this,
                    "Укажите серийный номер из этого акта (тип «" + BridgeDbHelper.GetBridgeProductTypeName() +
                    "»). Используйте подсказки при вводе.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtSerial.Focus();
                return;
            }
            if (root.Length == 0)
            {
                MessageBox.Show(this, "Укажите путь к папке для отчётов.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReportsPath.Focus();
                return;
            }

            string rootFull;
            try
            {
                rootFull = Path.GetFullPath(root.Trim());
            }
            catch
            {
                MessageBox.Show(this, "Некорректный путь к папке.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string actFolderName = BridgeLogArchiveService.SanitizeFolderName(act);
            string actRoot = Path.Combine(rootFull, actFolderName);
            Directory.CreateDirectory(actRoot);

            string reportPath = BridgeExcelReportHelper.GetReportPath(actRoot);
            bool dupFolder = BridgeLogArchiveService.SerialFolderHasSavedContent(actRoot, serial);
            bool dupExcel = false;
            string excelCheckError = null;
            try
            {
                dupExcel = BridgeExcelReportHelper.SerialExistsInReport(reportPath, serial);
            }
            catch (Exception ex)
            {
                excelCheckError = ex.Message;
            }

            if (dupFolder || dupExcel || excelCheckError != null)
            {
                var sb = new StringBuilder();
                if (dupFolder)
                    sb.AppendLine("• Папка для этого серийного номера уже содержит сохранённые файлы логов.");
                if (dupExcel)
                    sb.AppendLine("• Этот серийный номер уже есть в столбце «Серийный номер» в «Отчет_Bridge.xlsx».");
                if (excelCheckError != null)
                    sb.AppendLine("• Не удалось проверить Excel: " + excelCheckError);
                sb.AppendLine();
                sb.AppendLine("Продолжить сохранение?");
                var ask = MessageBox.Show(this, sb.ToString(), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (ask != DialogResult.Yes)
                    return;
            }

            string bridgeUrl = ConfigurationManager.AppSettings["BridgeBaseUrl"];
            if (string.IsNullOrWhiteSpace(bridgeUrl))
                bridgeUrl = "http://192.168.2.1";

            btnSaveLogs.Enabled = false;
            btnBrowseFolder.Enabled = false;
            txtReportsPath.Enabled = false;
            txtSerial.Enabled = false;
            UseWaitCursor = true;
            BridgeDownloadResult downloadResult = null;
            bool logsOk = false;
            bool dbOk = false;
            try
            {
                try
                {
                    downloadResult = await BridgeLogArchiveService.SaveLogsFromBridgeAsync(bridgeUrl, actRoot, serial).ConfigureAwait(true);
                    logsOk = true;
                }
                catch (Exception exNet)
                {
                    MessageBox.Show(this,
                        "Не удалось скачать логи с моста (Wi‑Fi, App.config BridgeBaseUrl, доступность устройства).\n" +
                        "Отчёт Excel всё равно будет создан или дополнен.\n\n" + exNet.Message,
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                try
                {
                    BridgeExcelReportHelper.AppendOperationRow(
                        BridgeExcelReportHelper.GetReportPath(actRoot),
                        act,
                        serial,
                        fio);
                }
                catch (Exception exExcel)
                {
                    MessageBox.Show(this,
                        "Не удалось обновить Excel (нужен Microsoft Office).\n" +
                        (logsOk ? "Логи с моста при этом сохранены в папку серийного номера.\n" : "") +
                        "Можно повторить попытку.\n\n" + exExcel.Message,
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    using (var ctx = ConnectionHelper.CreateContext())
                    {
                        ctx.InsertBridgeLogSave(
                            _actId,
                            _user.UserID,
                            serial,
                            downloadResult?.UnifiedText,
                            downloadResult?.StatusJson,
                            downloadResult?.MavlinkJson);
                    }
                    dbOk = true;
                }
                catch (Exception exDb)
                {
                    MessageBox.Show(this,
                        "Запись в Excel выполнена, но не удалось сохранить данные в БД.\n" +
                        "Если таблица ещё не создана, выполните скрипт Scripts/CreateBridgeLogSave.sql.\n\n" + exDb.Message,
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                txtSerial.Clear();

                string reportPathDone = BridgeExcelReportHelper.GetReportPath(actRoot);
                string logsNote = logsOk
                    ? "Логи с моста сохранены в папку серийного номера."
                    : "Логи с моста не сохранены — при необходимости повторите операцию после настройки сети.";
                string dbNote = dbOk
                    ? "Данные записаны в базу данных."
                    : "Запись в базу данных не выполнена — см. предупреждение выше.";
                MessageBox.Show(this,
                    "Готово. Серийный номер: «" + serial + "». Папка акта: «" + actFolderName + "».\n" +
                    logsNote + "\n" +
                    "Отчёт Excel:\n" + reportPathDone + "\n" +
                    dbNote + "\n" +
                    "Поле серийного номера очищено.",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            finally
            {
                UseWaitCursor = false;
                txtSerial.Enabled = _allowedSerials.Count > 0;
                txtReportsPath.Enabled = true;
                btnSaveLogs.Enabled = _allowedSerials.Count > 0;
                btnBrowseFolder.Enabled = true;
            }
        }
    }
}
