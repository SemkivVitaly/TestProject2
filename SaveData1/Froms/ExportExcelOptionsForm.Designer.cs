namespace SaveData1
{
    partial class ExportExcelOptionsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.radAll = new System.Windows.Forms.RadioButton();
            this.radSelected = new System.Windows.Forms.RadioButton();
            this.radByDate = new System.Windows.Forms.RadioButton();
            this.radByTime = new System.Windows.Forms.RadioButton();
            this.panelSelected = new System.Windows.Forms.Panel();
            this.clbActs = new System.Windows.Forms.CheckedListBox();
            this.lblSelectActs = new System.Windows.Forms.Label();
            this.panelByDate = new System.Windows.Forms.Panel();
            this.dtpDateTo = new System.Windows.Forms.DateTimePicker();
            this.dtpDateFrom = new System.Windows.Forms.DateTimePicker();
            this.lblDateTo = new System.Windows.Forms.Label();
            this.lblDateFrom = new System.Windows.Forms.Label();
            this.panelByTime = new System.Windows.Forms.Panel();
            this.dtpTimeTo = new System.Windows.Forms.DateTimePicker();
            this.dtpTimeFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTimeTo = new System.Windows.Forms.Label();
            this.lblTimeFrom = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelSelected.SuspendLayout();
            this.panelByDate.SuspendLayout();
            this.panelByTime.SuspendLayout();
            this.SuspendLayout();
            // 
            // radAll
            // 
            this.radAll.AutoSize = true;
            this.radAll.Checked = true;
            this.radAll.Location = new System.Drawing.Point(12, 12);
            this.radAll.Name = "radAll";
            this.radAll.Size = new System.Drawing.Size(72, 17);
            this.radAll.TabIndex = 0;
            this.radAll.TabStop = true;
            this.radAll.Text = "Все акты";
            this.radAll.UseVisualStyleBackColor = true;
            this.radAll.CheckedChanged += new System.EventHandler(this.rad_CheckedChanged);
            // 
            // radSelected
            // 
            this.radSelected.AutoSize = true;
            this.radSelected.Location = new System.Drawing.Point(12, 37);
            this.radSelected.Name = "radSelected";
            this.radSelected.Size = new System.Drawing.Size(117, 17);
            this.radSelected.TabIndex = 1;
            this.radSelected.Text = "Выборочные акты";
            this.radSelected.UseVisualStyleBackColor = true;
            this.radSelected.CheckedChanged += new System.EventHandler(this.rad_CheckedChanged);
            // 
            // radByDate
            // 
            this.radByDate.AutoSize = true;
            this.radByDate.Location = new System.Drawing.Point(12, 62);
            this.radByDate.Name = "radByDate";
            this.radByDate.Size = new System.Drawing.Size(65, 17);
            this.radByDate.TabIndex = 2;
            this.radByDate.Text = "По дате";
            this.radByDate.UseVisualStyleBackColor = true;
            this.radByDate.CheckedChanged += new System.EventHandler(this.rad_CheckedChanged);
            // 
            // radByTime
            // 
            this.radByTime.AutoSize = true;
            this.radByTime.Location = new System.Drawing.Point(12, 87);
            this.radByTime.Name = "radByTime";
            this.radByTime.Size = new System.Drawing.Size(86, 17);
            this.radByTime.TabIndex = 3;
            this.radByTime.Text = "По времени";
            this.radByTime.UseVisualStyleBackColor = true;
            this.radByTime.CheckedChanged += new System.EventHandler(this.rad_CheckedChanged);
            // 
            // panelSelected
            // 
            this.panelSelected.Controls.Add(this.clbActs);
            this.panelSelected.Controls.Add(this.lblSelectActs);
            this.panelSelected.Enabled = false;
            this.panelSelected.Location = new System.Drawing.Point(12, 115);
            this.panelSelected.Name = "panelSelected";
            this.panelSelected.Size = new System.Drawing.Size(280, 180);
            this.panelSelected.TabIndex = 4;
            this.panelSelected.Visible = false;
            // 
            // clbActs
            // 
            this.clbActs.CheckOnClick = true;
            this.clbActs.FormattingEnabled = true;
            this.clbActs.Location = new System.Drawing.Point(0, 25);
            this.clbActs.Name = "clbActs";
            this.clbActs.Size = new System.Drawing.Size(260, 139);
            this.clbActs.TabIndex = 1;
            // 
            // lblSelectActs
            // 
            this.lblSelectActs.AutoSize = true;
            this.lblSelectActs.Location = new System.Drawing.Point(0, 5);
            this.lblSelectActs.Name = "lblSelectActs";
            this.lblSelectActs.Size = new System.Drawing.Size(158, 13);
            this.lblSelectActs.TabIndex = 0;
            this.lblSelectActs.Text = "Отметьте акты для экспорта:";
            // 
            // panelByDate
            // 
            this.panelByDate.Controls.Add(this.dtpDateTo);
            this.panelByDate.Controls.Add(this.dtpDateFrom);
            this.panelByDate.Controls.Add(this.lblDateTo);
            this.panelByDate.Controls.Add(this.lblDateFrom);
            this.panelByDate.Enabled = false;
            this.panelByDate.Location = new System.Drawing.Point(12, 115);
            this.panelByDate.Name = "panelByDate";
            this.panelByDate.Size = new System.Drawing.Size(280, 80);
            this.panelByDate.TabIndex = 5;
            this.panelByDate.Visible = false;
            // 
            // dtpDateTo
            // 
            this.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateTo.Location = new System.Drawing.Point(80, 35);
            this.dtpDateTo.Name = "dtpDateTo";
            this.dtpDateTo.Size = new System.Drawing.Size(120, 20);
            this.dtpDateTo.TabIndex = 3;
            // 
            // dtpDateFrom
            // 
            this.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateFrom.Location = new System.Drawing.Point(80, 5);
            this.dtpDateFrom.Name = "dtpDateFrom";
            this.dtpDateFrom.Size = new System.Drawing.Size(120, 20);
            this.dtpDateFrom.TabIndex = 2;
            // 
            // lblDateTo
            // 
            this.lblDateTo.AutoSize = true;
            this.lblDateTo.Location = new System.Drawing.Point(0, 38);
            this.lblDateTo.Name = "lblDateTo";
            this.lblDateTo.Size = new System.Drawing.Size(24, 13);
            this.lblDateTo.TabIndex = 1;
            this.lblDateTo.Text = "По:";
            // 
            // lblDateFrom
            // 
            this.lblDateFrom.AutoSize = true;
            this.lblDateFrom.Location = new System.Drawing.Point(0, 8);
            this.lblDateFrom.Name = "lblDateFrom";
            this.lblDateFrom.Size = new System.Drawing.Size(17, 13);
            this.lblDateFrom.TabIndex = 0;
            this.lblDateFrom.Text = "С:";
            // 
            // panelByTime
            // 
            this.panelByTime.Controls.Add(this.dtpTimeTo);
            this.panelByTime.Controls.Add(this.dtpTimeFrom);
            this.panelByTime.Controls.Add(this.lblTimeTo);
            this.panelByTime.Controls.Add(this.lblTimeFrom);
            this.panelByTime.Enabled = false;
            this.panelByTime.Location = new System.Drawing.Point(12, 115);
            this.panelByTime.Name = "panelByTime";
            this.panelByTime.Size = new System.Drawing.Size(280, 80);
            this.panelByTime.TabIndex = 6;
            this.panelByTime.Visible = false;
            // 
            // dtpTimeTo
            // 
            this.dtpTimeTo.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTimeTo.Location = new System.Drawing.Point(80, 35);
            this.dtpTimeTo.Name = "dtpTimeTo";
            this.dtpTimeTo.ShowUpDown = true;
            this.dtpTimeTo.Size = new System.Drawing.Size(120, 20);
            this.dtpTimeTo.TabIndex = 3;
            // 
            // dtpTimeFrom
            // 
            this.dtpTimeFrom.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTimeFrom.Location = new System.Drawing.Point(80, 5);
            this.dtpTimeFrom.Name = "dtpTimeFrom";
            this.dtpTimeFrom.ShowUpDown = true;
            this.dtpTimeFrom.Size = new System.Drawing.Size(120, 20);
            this.dtpTimeFrom.TabIndex = 2;
            // 
            // lblTimeTo
            // 
            this.lblTimeTo.AutoSize = true;
            this.lblTimeTo.Location = new System.Drawing.Point(0, 38);
            this.lblTimeTo.Name = "lblTimeTo";
            this.lblTimeTo.Size = new System.Drawing.Size(22, 13);
            this.lblTimeTo.TabIndex = 1;
            this.lblTimeTo.Text = "до:";
            // 
            // lblTimeFrom
            // 
            this.lblTimeFrom.AutoSize = true;
            this.lblTimeFrom.Location = new System.Drawing.Point(0, 8);
            this.lblTimeFrom.Name = "lblTimeFrom";
            this.lblTimeFrom.Size = new System.Drawing.Size(52, 13);
            this.lblTimeFrom.TabIndex = 0;
            this.lblTimeFrom.Text = "Время с:";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(12, 305);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 28);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "Экспорт";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(118, 305);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 28);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ExportExcelOptionsForm
            // 
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(304, 345);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.panelByTime);
            this.Controls.Add(this.panelByDate);
            this.Controls.Add(this.panelSelected);
            this.Controls.Add(this.radByTime);
            this.Controls.Add(this.radByDate);
            this.Controls.Add(this.radSelected);
            this.Controls.Add(this.radAll);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportExcelOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Экспорт в Excel — выбор данных";
            this.panelSelected.ResumeLayout(false);
            this.panelSelected.PerformLayout();
            this.panelByDate.ResumeLayout(false);
            this.panelByDate.PerformLayout();
            this.panelByTime.ResumeLayout(false);
            this.panelByTime.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton radAll;
        private System.Windows.Forms.RadioButton radSelected;
        private System.Windows.Forms.RadioButton radByDate;
        private System.Windows.Forms.RadioButton radByTime;
        private System.Windows.Forms.Panel panelSelected;
        private System.Windows.Forms.CheckedListBox clbActs;
        private System.Windows.Forms.Label lblSelectActs;
        private System.Windows.Forms.Panel panelByDate;
        private System.Windows.Forms.DateTimePicker dtpDateTo;
        private System.Windows.Forms.DateTimePicker dtpDateFrom;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.Panel panelByTime;
        private System.Windows.Forms.DateTimePicker dtpTimeTo;
        private System.Windows.Forms.DateTimePicker dtpTimeFrom;
        private System.Windows.Forms.Label lblTimeTo;
        private System.Windows.Forms.Label lblTimeFrom;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
