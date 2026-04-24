using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SaveData1.CrossPlateTesting.Services;
using SaveData1.Entity;
using SaveData1.Helpers;
using SaveData1.Services;

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

        private async void QualityControlForm_Load(object sender, EventArgs e)
        {
            await this.RunWithWaitAsync(LoadGridAsync, "Загрузка списка", btnSave);
            txtScanBuffer.Focus();
        }

        private async System.Threading.Tasks.Task LoadGridAsync()
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = true;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", HeaderText = "ID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Категория", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Serial", HeaderText = "Серийный номер", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "State", HeaderText = "Состояние", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "NonConformity",
                HeaderText = "Ярлык несоответствия",
                FillWeight = 20,
                Text = "Создать ярлык",
                UseColumnTextForButtonValue = true
            });
            // Подписка выполняется один раз — при повторной загрузке колонки пересоздаются, а хэндлер
            // остаётся живым и продолжает работать с новым dgv.
            dgv.CellContentClick -= Dgv_OnCellContentClick;
            dgv.CellContentClick += Dgv_OnCellContentClick;

            string bridgeTypeName = BridgeDbHelper.GetBridgeProductTypeName();
            var rowItems = await DbOperation.RunAsync(ctx =>
            {
                var rows = ctx.TechnicalMapFull
                    .AsNoTracking()
                    .Include(f => f.Product)
                    .Include("Product.ProducType")
                    .Include("TechnicalMapAssembly")
                    .Include("TechnicalMapTesting")
                    .Where(f => f.Product.Act != null && f.Product.Act.ActNumber == _actNumber
                        && !f.Inspection
                        && f.Product.PostTestingWarehouseAt == null
                        && (f.TechnicalMapAssembly.Any(a => a.IsReady)
                            || (f.Product.ProducType != null
                                && (f.Product.ProducType.TypeName == ProductLifecycleValidation.PolletnikiProductTypeName
                                    || f.Product.ProducType.TypeName == CrossPlateDbHelper.CrossProductTypeName
                                    || f.Product.ProducType.TypeName == bridgeTypeName))))
                    .ToList();

                var list = new System.Collections.Generic.List<(int ProductID, string Category, string Serial, bool AlreadyQc)>();
                // Одна строка на продукт: при нескольких техкартах — последняя по TMID среди отфильтрованных выше.
                foreach (var f in rows.GroupBy(r => r.ProductID)
                             .Select(g => g.OrderByDescending(x => x.TMID).First())
                             .OrderBy(x => x.Product.ProductSerial))
                {
                    if (!ProductLifecycleValidation.LatestTestingSucceeded(ctx, f.ProductID))
                        continue;
                    list.Add((f.ProductID, f.Product.ProducType?.TypeName ?? "", f.Product.ProductSerial ?? "",
                        f.Product.QualityControlPassed));
                }
                return list;
            }, "QualityControlForm.LoadGrid");

            foreach (var item in rowItems)
            {
                int idx = dgv.Rows.Add(item.ProductID, item.Category, item.Serial,
                    item.AlreadyQc ? "Уже прошёл контроль (БД)" : "Ожидает скан");
                if (item.AlreadyQc)
                    dgv.Rows[idx].DefaultCellStyle.BackColor = Color.LightGray;
            }
        }

        /// <summary>Обработчик клика по колонке «Ярлык несоответствия».</summary>
        private void Dgv_OnCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!(dgv.Columns[e.ColumnIndex] is DataGridViewButtonColumn)) return;
            if (dgv.Columns[e.ColumnIndex].Name != "NonConformity") return;

            var row = dgv.Rows[e.RowIndex];
            if (row?.Cells["ProductID"]?.Value == null) return;

            int productId;
            try { productId = Convert.ToInt32(row.Cells["ProductID"].Value); }
            catch { return; }

            string serial = (row.Cells["Serial"].Value ?? "").ToString();
            string category = (row.Cells["Category"].Value ?? "").ToString();
            string fio = _currentUser?.UserName ?? "";

            // В окне контроля ярлык создаётся только в категории «Склад → приёмка» (PlaceID=1).
            // Это соответствует существующей логике: брак, обнаруженный на контроле, идёт в приёмку.
            using (var dlg = new NonConformityForm(productId, serial, category, _actNumber, fio,
                showPlaceChoice: false, fixedPlaceId: 1))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    row.DefaultCellStyle.BackColor = Color.LightCoral;
                    row.Cells["State"].Value = "Ярлык несоответствия создан";
                    lblStatus.Text = $"Ярлык создан для: {serial}";
                    // Удаляем из списка «отсканированных», чтобы продукт не попал в контроль:
                    _scannedProductIds.Remove(productId);
                    try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
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
                try { System.Media.SystemSounds.Beep.Play(); } catch { }
                txtScanBuffer.Focus();
                return;
            }

            lblStatus.Text = $"Не найдено в акте: {raw}";
            try { System.Media.SystemSounds.Hand.Play(); } catch { }
            txtScanBuffer.Focus();
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_scannedProductIds.Count == 0)
            {
                MessageBox.Show("Нет отсканированных в этой сессии позиций для сохранения.", "Контроль",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var (ok, result) = await this.RunWithWaitAsync(
                () => System.Threading.Tasks.Task.Run(() =>
                    ProductLifecycleService.MarkQualityControlPassed(_actNumber, _scannedProductIds, _currentUser.UserID)),
                "Сохранение контроля",
                btnSave);
            if (!ok) return;

            string msg = result.Saved > 0
                ? $"Сохранено записей: {result.Saved}." + (result.Skipped > 0 ? $" Пропущено (изменились данные в БД или не проходят проверку): {result.Skipped}." : "")
                : "Ни одна запись не сохранена: данные в базе не соответствуют условиям контроля (актуальный успешный тест, акт, не передано на склад).";
            MessageBox.Show(msg, "Контроль",
                MessageBoxButtons.OK, result.Saved > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result.Saved > 0)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
