namespace SaveData1
{
    partial class SoundTestForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.cmbDevices = new System.Windows.Forms.ComboBox();
            this.lblDevice = new System.Windows.Forms.Label();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.lblDbInfo = new System.Windows.Forms.Label();
            this.lblHzInfo = new System.Windows.Forms.Label();
            this.lblDb = new System.Windows.Forms.Label();
            this.lblHz = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // cmbDevices
            this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevices.FormattingEnabled = true;
            this.cmbDevices.Location = new System.Drawing.Point(12, 27);
            this.cmbDevices.Name = "cmbDevices";
            this.cmbDevices.Size = new System.Drawing.Size(260, 21);
            this.cmbDevices.TabIndex = 0;

            // lblDevice
            this.lblDevice.AutoSize = true;
            this.lblDevice.Location = new System.Drawing.Point(12, 9);
            this.lblDevice.Name = "lblDevice";
            this.lblDevice.Size = new System.Drawing.Size(126, 13);
            this.lblDevice.TabIndex = 1;
            this.lblDevice.Text = "Выберите микрофон:";

            // btnStartStop
            this.btnStartStop.Location = new System.Drawing.Point(12, 60);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(260, 35);
            this.btnStartStop.TabIndex = 2;
            this.btnStartStop.Text = "Старт";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);

            // lblDbInfo
            this.lblDbInfo.AutoSize = true;
            this.lblDbInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDbInfo.Location = new System.Drawing.Point(12, 110);
            this.lblDbInfo.Name = "lblDbInfo";
            this.lblDbInfo.Size = new System.Drawing.Size(83, 17);
            this.lblDbInfo.TabIndex = 3;
            this.lblDbInfo.Text = "Громкость:";

            // lblHzInfo
            this.lblHzInfo.AutoSize = true;
            this.lblHzInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblHzInfo.Location = new System.Drawing.Point(12, 140);
            this.lblHzInfo.Name = "lblHzInfo";
            this.lblHzInfo.Size = new System.Drawing.Size(67, 17);
            this.lblHzInfo.TabIndex = 4;
            this.lblHzInfo.Text = "Частота:";

            // lblDb
            this.lblDb = new System.Windows.Forms.Label();
            this.lblDb.AutoSize = true;
            this.lblDb.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDb.Location = new System.Drawing.Point(100, 110);
            this.lblDb.Name = "lblDb";
            this.lblDb.Size = new System.Drawing.Size(43, 17);
            this.lblDb.TabIndex = 5;
            this.lblDb.Text = "0 дБ";

            // lblHz
            this.lblHz = new System.Windows.Forms.Label();
            this.lblHz.AutoSize = true;
            this.lblHz.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblHz.Location = new System.Drawing.Point(100, 140);
            this.lblHz.Name = "lblHz";
            this.lblHz.Size = new System.Drawing.Size(42, 17);
            this.lblHz.TabIndex = 6;
            this.lblHz.Text = "0 Гц";

            // SoundTestForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 181);
            this.Controls.Add(this.lblHz);
            this.Controls.Add(this.lblDb);
            this.Controls.Add(this.lblHzInfo);
            this.Controls.Add(this.lblDbInfo);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.lblDevice);
            this.Controls.Add(this.cmbDevices);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SoundTestForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Тестирование звука";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SoundTestForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cmbDevices;
        private System.Windows.Forms.Label lblDevice;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Label lblDbInfo;
        private System.Windows.Forms.Label lblHzInfo;
        private System.Windows.Forms.Label lblDb;
        private System.Windows.Forms.Label lblHz;
    }
}