using System;
using System.Data.Entity;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

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

        private void PostTestingShipToWarehouseForm_Load(object sender, EventArgs e)
        {
            LoadGrid();
        }

        private void LoadGrid()
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

            using (var ctx = ConnectionHelper.CreateContext())
            {
                var rows = ctx.TechnicalMapFull
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
                    .ToList();

                foreach (var f in rows.OrderBy(x => x.Product.ProductSerial))
                {
                    var tst = f.TechnicalMapTesting?.OrderByDescending(t => t.TMTID).FirstOrDefault();
                    if (tst == null || !tst.IsReadt || tst.Fault)
                        continue;
                    string cat = f.Product.ProducType != null ? f.Product.ProducType.TypeName : "";
                    dgv.Rows.Add(f.ProductID, f.Product.ProductSerial, cat);
                }
            }

            lblCount.Text = $"Всего к передаче: {dgv.Rows.Count}";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("Нет продуктов для передачи на склад.", "Отгрузка",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Передать все перечисленные продукты на склад («После тестирования»)?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var utc = DateTime.UtcNow;
                int saved = 0;
                int skipped = 0;
                using (var ctx = ConnectionHelper.CreateContext())
                using (var tx = ctx.Database.BeginTransaction())
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.IsNewRow) continue;
                        int pid = Convert.ToInt32(row.Cells["ProductID"].Value);
                        var p = ctx.Product.Include(x => x.Act).FirstOrDefault(x => x.ProductID == pid);
                        if (p == null || p.ActID == null || p.Act == null || p.Act.ActNumber != _actNumber)
                        {
                            skipped++;
                            continue;
                        }
                        if (!p.QualityControlPassed || p.PostTestingWarehouseAt != null)
                        {
                            skipped++;
                            continue;
                        }
                        if (!ProductLifecycleValidation.LatestTestingSucceeded(ctx, pid))
                        {
                            skipped++;
                            continue;
                        }

                        p.PostTestingWarehouseAt = utc;
                        p.PostTestingWarehouseByUserID = _currentUser.UserID;
                        saved++;
                    }

                    ctx.SaveChanges();
                    tx.Commit();
                }

                if (saved == 0)
                {
                    MessageBox.Show("Ни одна запись не сохранена: в базе данные уже изменились или не выполнены условия (контроль, успешный тест).",
                        "Отгрузка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string msg = $"Передано на склад записей: {saved}." + (skipped > 0 ? $" Пропущено: {skipped}." : "");
                MessageBox.Show(msg, "Отгрузка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
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
