using System;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    /// <summary>Диалог смены статуса продукта (В работе / Готово). Доступен только администратору из грида продуктов.</summary>
    public partial class ChangeStatusForm : Form
    {
        private readonly int _tmId;

        public bool NewInProgress { get; private set; }
        public bool NewIsReady { get; private set; }

        public ChangeStatusForm(int tmId, bool currentInProgress, bool currentIsReady)
        {
            InitializeComponent();
            _tmId = tmId;

            cmbStatus.Items.Add(new StatusItem("В работе", true, false));
            cmbStatus.Items.Add(new StatusItem("Готово", false, true));
            cmbStatus.DisplayMember = "Display";

            if (currentIsReady)
                cmbStatus.SelectedIndex = 1;
            else
                cmbStatus.SelectedIndex = 0;
        }

        /// <summary>Записывает выбранный статус в TechnicalMapAssembly по TMAID.</summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            var item = cmbStatus.SelectedItem as StatusItem;
            if (item == null) return;

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var tm = context.TechnicalMapAssembly.Find(_tmId);
                    if (tm == null)
                    {
                        MessageBox.Show("Запись не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    tm.InProgress = item.InProgress;
                    tm.IsReady = item.IsReady;
                    context.SaveChanges();
                }
                NewInProgress = item.InProgress;
                NewIsReady = item.IsReady;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>Элемент комбобокса: отображаемый текст и соответствующие флаги InProgress/isReady для записи в БД.</summary>
        private class StatusItem
        {
            public string Display { get; }
            public bool InProgress { get; }
            public bool IsReady { get; }
            public StatusItem(string display, bool inProgress, bool isReady)
            {
                Display = display;
                InProgress = inProgress;
                IsReady = isReady;
            }
        }
    }
}
