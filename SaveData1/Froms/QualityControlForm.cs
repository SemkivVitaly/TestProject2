using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1.Froms
{
    /// <summary>Контроль качества по акту: скан серийников, сохранение только отсканированных в этой сессии.</summary>
    public partial class QualityControlForm : Form
    {
        private readonly string _actNumber;
        private readonly UsersProfile _currentUser;
        private readonly HashSet<int> _scannedProductIds = new HashSet<int>();

        public QualityControlForm(string actNumber, UsersProfile currentUser)
        {
            if (string.IsNullOrWhiteSpace(actNumber))
                throw new ArgumentException("actNumber");
            _actNumber = actNumber.Trim();
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            InitializeComponent();
            Text = $"Контроль — акт № {_actNumber}";
        }

        private void QualityControlForm_Load(object sender, EventArgs e)
        {
            LoadGrid();
            txtScanBuffer.Focus();
        }

        private void LoadGrid()
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = true;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", HeaderText = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Serial", HeaderText = "Серийный номер", FillWeight = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "State", HeaderText = "Состояние", FillWeight = 50 });

            using (var ctx = ConnectionHelper.CreateContext())
            {
                var rows = ctx.TechnicalMapFull
                    .AsNoTracking()
                    .Include(f => f.Product)
                    .Include("TechnicalMapAssembly")
                    .Include("TechnicalMapTesting")
                    .Where(f => f.Product.Act != null && f.Product.Act.ActNumber == _actNumber
                        && !f.Inspection
                        && f.TechnicalMapAssembly.Any(a => a.IsReady)
                        && f.Product.PostTestingWarehouseAt == null)
                    .ToList();

                foreach (var f in rows.OrderBy(x => x.Product.ProductSerial))
                {
                    var tst = f.TechnicalMapTesting?.OrderByDescending(t => t.TMTID).FirstOrDefault();
                    if (tst == null || !tst.IsReadt || tst.Fault)
                        continue;
                    bool alreadyQc = f.Product.QualityControlPassed;
                    int idx = dgv.Rows.Add(f.ProductID, f.Product.ProductSerial, alreadyQc ? "Уже прошёл контроль (БД)" : "Ожидает скан");
                    if (alreadyQc)
                        dgv.Rows[idx].DefaultCellStyle.BackColor = Color.LightGray;
                }
            }
        }

        private void txtScanBuffer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            string raw = (txtScanBuffer.Text ?? "").Trim();
            txtScanBuffer.Clear();
            if (string.IsNullOrEmpty(raw))
                return;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                var serial = (row.Cells["Serial"].Value ?? "").ToString().Trim();
                if (!string.Equals(serial, raw, StringComparison.OrdinalIgnoreCase))
                    continue;

                int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                _scannedProductIds.Add(pid);
                row.DefaultCellStyle.BackColor = Color.LightGreen;
                row.Cells["State"].Value = "Отсканирован (сессия)";
                lblStatus.Text = $"Скан: {serial}";
                return;
            }

            lblStatus.Text = $"Не найдено в акте: {raw}";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_scannedProductIds.Count == 0)
            {
                MessageBox.Show("Нет отсканированных в этой сессии позиций для сохранения.", "Контроль",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int saved = 0;
                int skipped = 0;
                using (var ctx = ConnectionHelper.CreateContext())
                using (var tx = ctx.Database.BeginTransaction())
                {
                    var utc = DateTime.UtcNow;
                    foreach (int pid in _scannedProductIds)
                    {
                        var p = ctx.Product.Include(x => x.Act).FirstOrDefault(x => x.ProductID == pid);
                        if (p == null || p.ActID == null || p.Act == null || p.Act.ActNumber != _actNumber)
                        {
                            skipped++;
                            continue;
                        }
                        if (p.PostTestingWarehouseAt != null)
                        {
                            skipped++;
                            continue;
                        }
                        if (!ProductLifecycleValidation.LatestTestingSucceeded(ctx, pid))
                        {
                            skipped++;
                            continue;
                        }

                        p.QualityControlPassed = true;
                        p.QualityControlPassedUtc = utc;
                        p.QualityControlByUserID = _currentUser.UserID;
                        saved++;
                    }

                    ctx.SaveChanges();
                    tx.Commit();
                }

                string msg = saved > 0
                    ? $"Сохранено записей: {saved}." + (skipped > 0 ? $" Пропущено (изменились данные в БД или не проходят проверку): {skipped}." : "")
                    : "Ни одна запись не сохранена: данные в базе не соответствуют условиям контроля (актуальный успешный тест, акт, не передано на склад).";
                MessageBox.Show(msg, "Контроль",
                    MessageBoxButtons.OK, saved > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (saved > 0)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
