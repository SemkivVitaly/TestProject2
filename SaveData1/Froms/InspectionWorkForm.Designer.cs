namespace SaveData1
{
    partial class InspectionWorkForm
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
            this.lblAct = new System.Windows.Forms.Label();
            this.txtAct = new System.Windows.Forms.TextBox();
            this.lblPlace = new System.Windows.Forms.Label();
            this.txtPlace = new System.Windows.Forms.TextBox();
            this.lblOriginalFault = new System.Windows.Forms.Label();
            this.txtOriginalFault = new System.Windows.Forms.TextBox();
            this.lblInspectorFindings = new System.Windows.Forms.Label();
            this.clbInspectorDescriptions = new System.Windows.Forms.CheckedListBox();
            this.lblResult = new System.Windows.Forms.Label();
            this.cmbResult = new System.Windows.Forms.ComboBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lblSerial
            this.lblSerial.AutoSize = true;
            this.lblSerial.Location = new System.Drawing.Point(12, 12);
            this.lblSerial.Name = "lblSerial";
            this.lblSerial.Text = "Серийный номер:";

            // txtSerial
            this.txtSerial.Location = new System.Drawing.Point(12, 30);
            this.txtSerial.ReadOnly = true;
            this.txtSerial.Size = new System.Drawing.Size(380, 23);

            // lblCategory
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(12, 58);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Text = "Категория:";

            // txtCategory
            this.txtCategory.Location = new System.Drawing.Point(12, 76);
            this.txtCategory.ReadOnly = true;
            this.txtCategory.Size = new System.Drawing.Size(380, 23);

            // lblAct
            this.lblAct.AutoSize = true;
            this.lblAct.Location = new System.Drawing.Point(12, 104);
            this.lblAct.Name = "lblAct";
            this.lblAct.Text = "Акт:";

            // txtAct
            this.txtAct.Location = new System.Drawing.Point(12, 122);
            this.txtAct.ReadOnly = true;
            this.txtAct.Size = new System.Drawing.Size(180, 23);

            // lblPlace
            this.lblPlace.AutoSize = true;
            this.lblPlace.Location = new System.Drawing.Point(210, 104);
            this.lblPlace.Name = "lblPlace";
            this.lblPlace.Text = "Место обнаружения:";

            // txtPlace
            this.txtPlace.Location = new System.Drawing.Point(210, 122);
            this.txtPlace.ReadOnly = true;
            this.txtPlace.Size = new System.Drawing.Size(182, 23);

            // lblOriginalFault
            this.lblOriginalFault.AutoSize = true;
            this.lblOriginalFault.Location = new System.Drawing.Point(12, 152);
            this.lblOriginalFault.Name = "lblOriginalFault";
            this.lblOriginalFault.Text = "Обнаруженная неисправность:";

            // txtOriginalFault
            this.txtOriginalFault.Location = new System.Drawing.Point(12, 170);
            this.txtOriginalFault.Multiline = true;
            this.txtOriginalFault.ReadOnly = true;
            this.txtOriginalFault.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtOriginalFault.Size = new System.Drawing.Size(380, 55);

            // lblInspectorFindings
            this.lblInspectorFindings.AutoSize = true;
            this.lblInspectorFindings.Location = new System.Drawing.Point(12, 232);
            this.lblInspectorFindings.Name = "lblInspectorFindings";
            this.lblInspectorFindings.Text = "Неисправности (инспектор):";

            // clbInspectorDescriptions
            this.clbInspectorDescriptions.CheckOnClick = true;
            this.clbInspectorDescriptions.FormattingEnabled = true;
            this.clbInspectorDescriptions.Location = new System.Drawing.Point(12, 250);
            this.clbInspectorDescriptions.Size = new System.Drawing.Size(380, 112);

            // lblResult
            this.lblResult.AutoSize = true;
            this.lblResult.Location = new System.Drawing.Point(12, 370);
            this.lblResult.Name = "lblResult";
            this.lblResult.Text = "Результат проверки:";

            // cmbResult
            this.cmbResult.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResult.FormattingEnabled = true;
            this.cmbResult.Location = new System.Drawing.Point(12, 388);
            this.cmbResult.Size = new System.Drawing.Size(380, 23);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(12, 425);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(180, 32);
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(212, 425);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(180, 32);
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // InspectionWorkForm
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(408, 470);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.cmbResult);
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.clbInspectorDescriptions);
            this.Controls.Add(this.lblInspectorFindings);
            this.Controls.Add(this.txtOriginalFault);
            this.Controls.Add(this.lblOriginalFault);
            this.Controls.Add(this.txtPlace);
            this.Controls.Add(this.lblPlace);
            this.Controls.Add(this.txtAct);
            this.Controls.Add(this.lblAct);
            this.Controls.Add(this.txtCategory);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.txtSerial);
            this.Controls.Add(this.lblSerial);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InspectionWorkForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Инспекция продукта";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblSerial;
        private System.Windows.Forms.TextBox txtSerial;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.TextBox txtCategory;
        private System.Windows.Forms.Label lblAct;
        private System.Windows.Forms.TextBox txtAct;
        private System.Windows.Forms.Label lblPlace;
        private System.Windows.Forms.TextBox txtPlace;
        private System.Windows.Forms.Label lblOriginalFault;
        private System.Windows.Forms.TextBox txtOriginalFault;
        private System.Windows.Forms.Label lblInspectorFindings;
        private System.Windows.Forms.CheckedListBox clbInspectorDescriptions;
        private System.Windows.Forms.Label lblResult;
        private System.Windows.Forms.ComboBox cmbResult;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
