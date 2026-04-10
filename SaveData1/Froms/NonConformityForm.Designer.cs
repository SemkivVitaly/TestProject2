namespace SaveData1
{
    partial class NonConformityForm
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
            this.grpPlace = new System.Windows.Forms.GroupBox();
            this.rbReceiving = new System.Windows.Forms.RadioButton();
            this.rbShipping = new System.Windows.Forms.RadioButton();
            this.lblDescriptions = new System.Windows.Forms.Label();
            this.clbDescriptions = new System.Windows.Forms.CheckedListBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpPlace.SuspendLayout();
            this.SuspendLayout();

            // lblSerial
            this.lblSerial.AutoSize = true;
            this.lblSerial.Location = new System.Drawing.Point(12, 15);
            this.lblSerial.Name = "lblSerial";
            this.lblSerial.Size = new System.Drawing.Size(100, 15);
            this.lblSerial.Text = "Серийный номер:";

            // txtSerial
            this.txtSerial.Location = new System.Drawing.Point(12, 33);
            this.txtSerial.Name = "txtSerial";
            this.txtSerial.ReadOnly = true;
            this.txtSerial.Size = new System.Drawing.Size(340, 23);

            // lblCategory
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(12, 62);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(65, 15);
            this.lblCategory.Text = "Категория:";

            // txtCategory
            this.txtCategory.Location = new System.Drawing.Point(12, 80);
            this.txtCategory.Name = "txtCategory";
            this.txtCategory.ReadOnly = true;
            this.txtCategory.Size = new System.Drawing.Size(340, 23);

            // lblAct
            this.lblAct.AutoSize = true;
            this.lblAct.Location = new System.Drawing.Point(12, 109);
            this.lblAct.Name = "lblAct";
            this.lblAct.Size = new System.Drawing.Size(28, 15);
            this.lblAct.Text = "Акт:";

            // txtAct
            this.txtAct.Location = new System.Drawing.Point(12, 127);
            this.txtAct.Name = "txtAct";
            this.txtAct.ReadOnly = true;
            this.txtAct.Size = new System.Drawing.Size(340, 23);

            // grpPlace
            this.grpPlace.Controls.Add(this.rbReceiving);
            this.grpPlace.Controls.Add(this.rbShipping);
            this.grpPlace.Location = new System.Drawing.Point(12, 158);
            this.grpPlace.Name = "grpPlace";
            this.grpPlace.Size = new System.Drawing.Size(340, 50);
            this.grpPlace.TabStop = false;
            this.grpPlace.Text = "Место обнаружения";

            // rbReceiving
            this.rbReceiving.AutoSize = true;
            this.rbReceiving.Checked = true;
            this.rbReceiving.Location = new System.Drawing.Point(15, 22);
            this.rbReceiving.Name = "rbReceiving";
            this.rbReceiving.Size = new System.Drawing.Size(78, 19);
            this.rbReceiving.TabStop = true;
            this.rbReceiving.Text = "Приёмка";

            // rbShipping
            this.rbShipping.AutoSize = true;
            this.rbShipping.Location = new System.Drawing.Point(170, 22);
            this.rbShipping.Name = "rbShipping";
            this.rbShipping.Size = new System.Drawing.Size(79, 19);
            this.rbShipping.Text = "Отгрузка";

            // lblDescriptions
            this.lblDescriptions.AutoSize = true;
            this.lblDescriptions.Location = new System.Drawing.Point(12, 215);
            this.lblDescriptions.Name = "lblDescriptions";
            this.lblDescriptions.Size = new System.Drawing.Size(167, 15);
            this.lblDescriptions.Text = "В чём заключается неисправность:";

            // clbDescriptions
            this.clbDescriptions.CheckOnClick = true;
            this.clbDescriptions.FormattingEnabled = true;
            this.clbDescriptions.Location = new System.Drawing.Point(12, 233);
            this.clbDescriptions.Name = "clbDescriptions";
            this.clbDescriptions.Size = new System.Drawing.Size(340, 112);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(12, 355);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(160, 30);
            this.btnSave.Text = "Создать ярлык";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(192, 355);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(160, 30);
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // NonConformityForm
            this.AcceptButton = this.btnSave;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(370, 398);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.clbDescriptions);
            this.Controls.Add(this.lblDescriptions);
            this.Controls.Add(this.grpPlace);
            this.Controls.Add(this.txtAct);
            this.Controls.Add(this.lblAct);
            this.Controls.Add(this.txtCategory);
            this.Controls.Add(this.lblCategory);
            this.Controls.Add(this.txtSerial);
            this.Controls.Add(this.lblSerial);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NonConformityForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Ярлык несоответствия";
            this.grpPlace.ResumeLayout(false);
            this.grpPlace.PerformLayout();
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
        private System.Windows.Forms.GroupBox grpPlace;
        private System.Windows.Forms.RadioButton rbReceiving;
        private System.Windows.Forms.RadioButton rbShipping;
        private System.Windows.Forms.Label lblDescriptions;
        private System.Windows.Forms.CheckedListBox clbDescriptions;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
