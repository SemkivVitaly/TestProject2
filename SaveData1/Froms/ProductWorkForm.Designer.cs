namespace SaveData1
{
    partial class ProductWorkForm
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
            this.lblSerial = new System.Windows.Forms.Label();
            this.txtSerial = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.txtCategory = new System.Windows.Forms.TextBox();
            this.lblFIO = new System.Windows.Forms.Label();
            this.txtFIO = new System.Windows.Forms.TextBox();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblTimeStart = new System.Windows.Forms.Label();
            this.dtpTimeStart = new System.Windows.Forms.DateTimePicker();
            this.lblTimeEnd = new System.Windows.Forms.Label();
            this.dtpTimeEnd = new System.Windows.Forms.DateTimePicker();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSoundTest = new System.Windows.Forms.Button();
            this.chkFault = new System.Windows.Forms.CheckBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.clbDescription = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();

            // lblSerial
            this.lblSerial.AutoSize = true;
            this.lblSerial.Location = new System.Drawing.Point(12, 15);
            this.lblSerial.Name = "lblSerial";
            this.lblSerial.Size = new System.Drawing.Size(100, 15);
            this.lblSerial.TabIndex = 0;
            this.lblSerial.Text = "Серийный номер:";

            // txtSerial
            this.txtSerial.Location = new System.Drawing.Point(12, 33);
            this.txtSerial.Name = "txtSerial";
            this.txtSerial.ReadOnly = true;
            this.txtSerial.Size = new System.Drawing.Size(320, 23);
            this.txtSerial.TabIndex = 1;

            // lblCategory
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(12, 65);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(65, 15);
            this.lblCategory.TabIndex = 2;
            this.lblCategory.Text = "Категория:";

            // txtCategory
            this.txtCategory.Location = new System.Drawing.Point(12, 83);
            this.txtCategory.Name = "txtCategory";
            this.txtCategory.ReadOnly = true;
            this.txtCategory.Size = new System.Drawing.Size(320, 23);
            this.txtCategory.TabIndex = 3;

            // lblFIO
            this.lblFIO.AutoSize = true;
            this.lblFIO.Location = new System.Drawing.Point(12, 115);
            this.lblFIO.Name = "lblFIO";
            this.lblFIO.Size = new System.Drawing.Size(34, 15);
            this.lblFIO.TabIndex = 4;
            this.lblFIO.Text = "ФИО:";

            // txtFIO
            this.txtFIO.Location = new System.Drawing.Point(12, 133);
            this.txtFIO.Name = "txtFIO";
            this.txtFIO.ReadOnly = true;
            this.txtFIO.Size = new System.Drawing.Size(320, 23);
            this.txtFIO.TabIndex = 5;

            // lblDate
            this.lblDate.AutoSize = true;
            this.lblDate.Location = new System.Drawing.Point(12, 165);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(35, 15);
            this.lblDate.TabIndex = 6;
            this.lblDate.Text = "Дата:";

            // dtpDate
            this.dtpDate.Enabled = false;
            this.dtpDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDate.Location = new System.Drawing.Point(12, 183);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(120, 23);
            this.dtpDate.TabIndex = 7;

            // lblTimeStart
            this.lblTimeStart.AutoSize = true;
            this.lblTimeStart.Location = new System.Drawing.Point(12, 215);
            this.lblTimeStart.Name = "lblTimeStart";
            this.lblTimeStart.Size = new System.Drawing.Size(89, 15);
            this.lblTimeStart.TabIndex = 8;
            this.lblTimeStart.Text = "Время начала:";

            // dtpTimeStart
            this.dtpTimeStart.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTimeStart.Location = new System.Drawing.Point(12, 233);
            this.dtpTimeStart.Name = "dtpTimeStart";
            this.dtpTimeStart.ShowUpDown = true;
            this.dtpTimeStart.Size = new System.Drawing.Size(120, 23);
            this.dtpTimeStart.TabIndex = 9;

            // lblTimeEnd
            this.lblTimeEnd.AutoSize = true;
            this.lblTimeEnd.Location = new System.Drawing.Point(12, 265);
            this.lblTimeEnd.Name = "lblTimeEnd";
            this.lblTimeEnd.Size = new System.Drawing.Size(102, 15);
            this.lblTimeEnd.TabIndex = 10;
            this.lblTimeEnd.Text = "Время окончания:";

            // dtpTimeEnd
            this.dtpTimeEnd.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTimeEnd.Location = new System.Drawing.Point(12, 283);
            this.dtpTimeEnd.Name = "dtpTimeEnd";
            this.dtpTimeEnd.ShowUpDown = true;
            this.dtpTimeEnd.Size = new System.Drawing.Size(120, 23);
            this.dtpTimeEnd.TabIndex = 11;

            // chkFault
            this.chkFault.AutoSize = true;
            this.chkFault.Location = new System.Drawing.Point(12, 315);
            this.chkFault.Name = "chkFault";
            this.chkFault.Size = new System.Drawing.Size(155, 19);
            this.chkFault.TabIndex = 14;
            this.chkFault.Text = "Найдена неисправность";
            this.chkFault.UseVisualStyleBackColor = true;
            this.chkFault.CheckedChanged += new System.EventHandler(this.chkFault_CheckedChanged);

            // lblDescription
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(12, 340);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(161, 15);
            this.lblDescription.TabIndex = 15;
            this.lblDescription.Text = "Причина неисправности:";
            this.lblDescription.Visible = false;

            // clbDescription
            this.clbDescription.CheckOnClick = true;
            this.clbDescription.FormattingEnabled = true;
            this.clbDescription.Location = new System.Drawing.Point(12, 358);
            this.clbDescription.Name = "clbDescription";
            this.clbDescription.Size = new System.Drawing.Size(320, 94);
            this.clbDescription.TabIndex = 16;
            this.clbDescription.Visible = false;

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(12, 465);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 28);
            this.btnSave.TabIndex = 12;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(118, 465);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 28);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // btnSoundTest
            this.btnSoundTest.Location = new System.Drawing.Point(224, 465);
            this.btnSoundTest.Name = "btnSoundTest";
            this.btnSoundTest.Size = new System.Drawing.Size(120, 28);
            this.btnSoundTest.TabIndex = 15;
            this.btnSoundTest.Text = "Проверить звук";
            this.btnSoundTest.UseVisualStyleBackColor = true;
            this.btnSoundTest.Click += new System.EventHandler(this.btnSoundTest_Click);

            // ProductWorkForm
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(356, 506);
            this.Controls.Add(this.clbDescription);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.chkFault);
            this.Controls.Add(this.btnSoundTest);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.dtpTimeEnd);
            this.Controls.Add(this.lblTimeEnd);
            this.Controls.Add(this.dtpTimeStart);
            this.Controls.Add(this.lblTimeStart);
            this.Controls.Add(this.dtpDate);
            this.Controls.Add(this.lblDate);
            this.Controls.Add(this.txtFIO);
            this.Controls.Add(this.lblFIO);
            this.Controls.Add(this.txtCategory);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.txtSerial);
            this.Controls.Add(this.lblSerial);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProductWorkForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Работа с продуктом";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblSerial;
        private System.Windows.Forms.TextBox txtSerial;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.TextBox txtCategory;
        private System.Windows.Forms.Label lblFIO;
        private System.Windows.Forms.TextBox txtFIO;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.Label lblTimeStart;
        private System.Windows.Forms.DateTimePicker dtpTimeStart;
        private System.Windows.Forms.Label lblTimeEnd;
        private System.Windows.Forms.DateTimePicker dtpTimeEnd;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSoundTest;
        private System.Windows.Forms.CheckBox chkFault;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.CheckedListBox clbDescription;
    }
}
