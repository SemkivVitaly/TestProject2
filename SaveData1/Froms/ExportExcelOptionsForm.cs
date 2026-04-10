using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Helpers;

namespace SaveData1
{
    /// <summary>Режим экспорта в Excel: все акты, выборочные акты, по диапазону дат или по времени начала работы.</summary>
    public enum ExportExcelMode
    {
        All,
        SelectedActs,
        ByDate,
        ByTime
    }

    /// <summary>Диалог выбора данных для экспорта в Excel. Панели выбора актов/дат/времени переключаются по радиокнопкам и выводятся поверх друг друга (BringToFront).</summary>
    public partial class ExportExcelOptionsForm : Form
    {
        public ExportExcelMode Mode { get; private set; }
        public List<string> SelectedActNumbers { get; private set; }
        public DateTime DateFrom { get; private set; }
        public DateTime DateTo { get; private set; }
        public TimeSpan TimeFrom { get; private set; }
        public TimeSpan TimeTo { get; private set; }

        public ExportExcelOptionsForm()
        {
            InitializeComponent();
            dtpDateFrom.Value = DateTime.Today.AddMonths(-1);
            dtpDateTo.Value = DateTime.Today;
            dtpTimeFrom.Value = DateTime.Today;
            dtpTimeTo.Value = DateTime.Today.AddHours(23).AddMinutes(59);
            LoadActs();
        }

        /// <summary>Заполняет список актов в CheckedListBox для режима «Выборочные акты».</summary>
        private void LoadActs()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var acts = context.Act.OrderBy(a => a.ActNumber).Select(a => a.ActNumber).ToList();
                    clbActs.Items.Clear();
                    foreach (var an in acts)
                        clbActs.Items.Add(an, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки списка актов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Показ только активной панели (выбор актов / дат / времени), т.к. панели в одной области
        private void rad_CheckedChanged(object sender, EventArgs e)
        {
            panelSelected.Visible = radSelected.Checked;
            panelSelected.Enabled = radSelected.Checked;
            if (radSelected.Checked)
                panelSelected.BringToFront();

            panelByDate.Visible = radByDate.Checked;
            panelByDate.Enabled = radByDate.Checked;
            if (radByDate.Checked)
                panelByDate.BringToFront();

            panelByTime.Visible = radByTime.Checked;
            panelByTime.Enabled = radByTime.Checked;
            if (radByTime.Checked)
                panelByTime.BringToFront();
        }

        /// <summary>Проверяет ввод (при выборочных — хотя бы один акт; при по дате — дата «по» >= «с») и запоминает выбранный режим и параметры для экспорта.</summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (radSelected.Checked)
            {
                SelectedActNumbers = new List<string>();
                for (int i = 0; i < clbActs.Items.Count; i++)
                {
                    if (clbActs.GetItemChecked(i))
                        SelectedActNumbers.Add(clbActs.Items[i].ToString());
                }
                if (SelectedActNumbers.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один акт.", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Mode = ExportExcelMode.SelectedActs;
            }
            else if (radByDate.Checked)
            {
                DateFrom = dtpDateFrom.Value.Date;
                DateTo = dtpDateTo.Value.Date;
                if (DateTo < DateFrom)
                {
                    MessageBox.Show("Дата «по» не может быть раньше даты «с».", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Mode = ExportExcelMode.ByDate;
            }
            else if (radByTime.Checked)
            {
                TimeFrom = dtpTimeFrom.Value.TimeOfDay;
                TimeTo = dtpTimeTo.Value.TimeOfDay;
                Mode = ExportExcelMode.ByTime;
            }
            else
            {
                Mode = ExportExcelMode.All;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
