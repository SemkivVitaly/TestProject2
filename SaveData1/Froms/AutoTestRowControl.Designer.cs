namespace SaveData1.Froms
{
    partial class AutoTestRowControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtSerial = new System.Windows.Forms.TextBox();
            this.cmbUsb = new System.Windows.Forms.ComboBox();
            this.btnRefreshUsb = new System.Windows.Forms.Button();
            this.lblStand = new System.Windows.Forms.Label();
            this.txtStand = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnError = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtSerial
            // 
            this.txtSerial.Location = new System.Drawing.Point(2, 4);
            this.txtSerial.Margin = new System.Windows.Forms.Padding(2);
            this.txtSerial.Name = "txtSerial";
            this.txtSerial.Size = new System.Drawing.Size(91, 20);
            this.txtSerial.TabIndex = 0;
            // 
            // cmbUsb
            // 
            this.cmbUsb.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbUsb.FormattingEnabled = true;
            this.cmbUsb.Location = new System.Drawing.Point(97, 4);
            this.cmbUsb.Margin = new System.Windows.Forms.Padding(2);
            this.cmbUsb.Name = "cmbUsb";
            this.cmbUsb.Size = new System.Drawing.Size(114, 21);
            this.cmbUsb.TabIndex = 1;
            // 
            // btnRefreshUsb
            // 
            this.btnRefreshUsb.Location = new System.Drawing.Point(214, 3);
            this.btnRefreshUsb.Margin = new System.Windows.Forms.Padding(2);
            this.btnRefreshUsb.Name = "btnRefreshUsb";
            this.btnRefreshUsb.Size = new System.Drawing.Size(19, 20);
            this.btnRefreshUsb.TabIndex = 2;
            this.btnRefreshUsb.Text = "↻";
            this.btnRefreshUsb.UseVisualStyleBackColor = true;
            this.btnRefreshUsb.Click += new System.EventHandler(this.btnRefreshUsb_Click);
            // 
            // lblStand
            // 
            this.lblStand.AutoSize = true;
            this.lblStand.Location = new System.Drawing.Point(236, 6);
            this.lblStand.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblStand.Name = "lblStand";
            this.lblStand.Size = new System.Drawing.Size(51, 13);
            this.lblStand.TabIndex = 3;
            this.lblStand.Text = "Стенд №";
            // 
            // txtStand
            // 
            this.txtStand.Location = new System.Drawing.Point(293, 4);
            this.txtStand.Margin = new System.Windows.Forms.Padding(2);
            this.txtStand.Name = "txtStand";
            this.txtStand.Size = new System.Drawing.Size(46, 20);
            this.txtStand.TabIndex = 4;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(343, 3);
            this.btnSave.Margin = new System.Windows.Forms.Padding(2);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(83, 20);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(430, 3);
            this.btnRemove.Margin = new System.Windows.Forms.Padding(2);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(77, 20);
            this.btnRemove.TabIndex = 6;
            this.btnRemove.Text = "Удалить";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnError
            // 
            this.btnError.Location = new System.Drawing.Point(510, 3);
            this.btnError.Margin = new System.Windows.Forms.Padding(2);
            this.btnError.Name = "btnError";
            this.btnError.Size = new System.Drawing.Size(60, 20);
            this.btnError.TabIndex = 8;
            this.btnError.Text = "Ошибка";
            this.btnError.UseVisualStyleBackColor = true;
            this.btnError.Click += new System.EventHandler(this.btnError_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(444, 6);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 7;
            // 
            // AutoTestRowControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnError);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtStand);
            this.Controls.Add(this.lblStand);
            this.Controls.Add(this.btnRefreshUsb);
            this.Controls.Add(this.cmbUsb);
            this.Controls.Add(this.txtSerial);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "AutoTestRowControl";
            this.Size = new System.Drawing.Size(807, 26);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TextBox txtSerial;
        private System.Windows.Forms.ComboBox cmbUsb;
        private System.Windows.Forms.Button btnRefreshUsb;
        private System.Windows.Forms.Label lblStand;
        private System.Windows.Forms.TextBox txtStand;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnError;
        private System.Windows.Forms.Label lblStatus;
    }
}