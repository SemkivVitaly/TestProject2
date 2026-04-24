using System;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    /// <summary>Диалог смены статуса продукта (В работе / Готово). Доступен только администратору из грида продуктов.</summary>
    public partial class ChangeStatusForm : Form
    {
        private readonly int _recordId;
        /// <summary>true — правим <see cref="Entity.TechnicalMapTesting"/> (TMTID); false — <see cref="Entity.TechnicalMapAssembly"/> (TMAID).</summary>
        private readonly bool _isTechnicalMapTesting;

        public bool NewInProgress { get; private set; }
        public bool NewIsReady { get; private set; }

        public ChangeStatusForm(int recordId, bool currentInProgress, bool currentIsReady, bool isTechnicalMapTesting = false)
        {
            InitializeComponent();
            _recordId = recordId;
            _isTechnicalMapTesting = isTechnicalMapTesting;

            cmbStatus.Items.Add(new StatusItem("В работе", true, false));
            cmbStatus.Items.Add(new StatusItem("Готово", false, true));
            cmbStatus.DisplayMember = "Display";

            if (currentIsReady)
                cmbStatus.SelectedIndex = 1;
            else
                cmbStatus.SelectedIndex = 0;
        }

        /// <summary>Записывает статус в TechnicalMapAssembly (сборка) или TechnicalMapTesting (тест / полётники / кросс-платы / Bridge).</summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            var item = cmbStatus.SelectedItem as StatusItem;
            if (item == null) return;

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    if (_isTechnicalMapTesting)
                    {
                        var tst = context.TechnicalMapTesting.Find(_recordId);
                        if (tst == null)
                        {
                            MessageBox.Show("Запись тестирования не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        tst.InProgress = item.InProgress;
                        tst.IsReadt = item.IsReady;
                        if (item.IsReady)
                            tst.Fault = false;
                        var now = DateTime.Now;
                        tst.Date = now;
                        if (item.InProgress && tst.TimeStart == TimeSpan.Zero)
                            tst.TimeStart = now.TimeOfDay;
                        tst.TimeEnd = now.TimeOfDay;
                        context.SaveChanges();
                    }
                    else
                    {
                        var tm = context.TechnicalMapAssembly.Find(_recordId);
                        if (tm == null)
                        {
                            MessageBox.Show("Запись сборки не найдена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        tm.InProgress = item.InProgress;
                        tm.IsReady = item.IsReady;
                        context.SaveChanges();
                    }
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
