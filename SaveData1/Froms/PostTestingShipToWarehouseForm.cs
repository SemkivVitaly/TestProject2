using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;
using SaveData1.Services;

namespace SaveData1.Froms
{
    /// <summary>Передача на склад после теста и контроля: все подходящие серийники акта.</summary>
    public partial class PostTestingShipToWarehouseForm : Form
    {
        private readonly string _actNumber;
        private readonly UsersProfile _currentUser;

        public PostTestingShipToWarehouseForm(string actNumber, UsersProfile currentUser)
        {
            if (string.IsNullOrWhiteSpace(actNumber))
                throw new ArgumentException("actNumber");
            _actNumber = actNumber.Trim();
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            InitializeComponent();
            Text = $"Отгрузка на склад — акт № {_actNumber}";
        }

        private async void PostTestingShipToWarehouseForm_Load(object sender, EventArgs e)
        {
            await this.RunWithWaitAsync(LoadGridAsync, "Загрузка списка", btnSave);
        }

        private async System.Threading.Tasks.Task LoadGridAsync()
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Serial", HeaderText = "Серийный номер", FillWeight = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Категория", FillWeight = 40 });

            var rows = await DbOperation.RunAsync(ctx => ctx.TechnicalMapFull
                .AsNoTracking()
                .Include(f => f.Product)
                .Include(f => f.Product.ProducType)
                .Include("TechnicalMapAssembly")
                .Include("TechnicalMapTesting")
                .Where(f => f.Product.Act != null && f.Product.Act.ActNumber == _actNumber
                    && !f.Inspection
                    && f.TechnicalMapAssembly.Any(a => a.IsReady)
                    && f.Product.QualityControlPassed
                    && f.Product.PostTestingWarehouseAt == null)
                .ToList(),
                "PostTestingShipToWarehouseForm.LoadGrid");

            foreach (var f in rows.OrderBy(x => x.Product.ProductSerial))
            {
                var tst = f.TechnicalMapTesting?.OrderByDescending(t => t.TMTID).FirstOrDefault();
                if (tst == null || !tst.IsReadt || tst.Fault)
                    continue;
                string cat = f.Product.ProducType != null ? f.Product.ProducType.TypeName : "";
                dgv.Rows.Add(f.ProductID, f.Product.ProductSerial, cat);
            }

            lblCount.Text = $"Всего к передаче: {dgv.Rows.Count}";
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("Нет продуктов для передачи на склад.", "Отгрузка",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var productIds = new List<int>(dgv.Rows.Count);
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                productIds.Add(Convert.ToInt32(row.Cells["ProductID"].Value));
            }

            if (MessageBox.Show($"Передать {productIds.Count} продукт(ов) на склад («После тестирования»)?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var (ok, result) = await this.RunWithWaitAsync(
                () => System.Threading.Tasks.Task.Run(() =>
                    ProductLifecycleService.ShipToPostTestingWarehouse(_actNumber, productIds, _currentUser.UserID)),
                "Передача на склад",
                btnSave);
            if (!ok) return;

            if (result.Saved == 0)
            {
                MessageBox.Show("Ни одна запись не сохранена: в базе данные уже изменились или не выполнены условия (контроль, успешный тест).",
                    "Отгрузка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string msg = $"Передано на склад записей: {result.Saved}." + (result.Skipped > 0 ? $" Пропущено: {result.Skipped}." : "");
            MessageBox.Show(msg, "Отгрузка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
