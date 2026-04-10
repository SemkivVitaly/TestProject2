namespace SaveData1.Froms
{
    partial class BridgeLogForm
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
            this.lblAct = new System.Windows.Forms.Label();
            this.lblEmployee = new System.Windows.Forms.Label();
            this.labelPath = new System.Windows.Forms.Label();
            this.txtReportsPath = new System.Windows.Forms.TextBox();
            this.btnBrowseFolder = new System.Windows.Forms.Button();
            this.labelQueue = new System.Windows.Forms.Label();
            this.lblSerial = new System.Windows.Forms.Label();
            this.txtSerial = new System.Windows.Forms.TextBox();
            this.btnSaveLogs = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblAct
            //
            this.lblAct.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAct.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblAct.Location = new System.Drawing.Point(18, 12);
            this.lblAct.Name = "lblAct";
            this.lblAct.Size = new System.Drawing.Size(602, 22);
            this.lblAct.TabIndex = 0;
            this.lblAct.Text = "Акт №";
            //
            // lblEmployee
            //
            this.lblEmployee.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEmployee.Location = new System.Drawing.Point(18, 38);
            this.lblEmployee.Name = "lblEmployee";
            this.lblEmployee.Size = new System.Drawing.Size(602, 22);
            this.lblEmployee.TabIndex = 1;
            this.lblEmployee.Text = "Исполнитель:";
            //
            // labelPath
            //
            this.labelPath.AutoSize = true;
            this.labelPath.Location = new System.Drawing.Point(18, 70);
            this.labelPath.MaximumSize = new System.Drawing.Size(180, 0);
            this.labelPath.Name = "labelPath";
            this.labelPath.Size = new System.Drawing.Size(163, 30);
            this.labelPath.TabIndex = 2;
            this.labelPath.Text = "Корневая папка (внутри — папка акта):";
            //
            // txtReportsPath
            //
            this.txtReportsPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtReportsPath.Location = new System.Drawing.Point(200, 67);
            this.txtReportsPath.Name = "txtReportsPath";
            this.txtReportsPath.Size = new System.Drawing.Size(310, 23);
            this.txtReportsPath.TabIndex = 3;
            //
            // btnBrowseFolder
            //
            this.btnBrowseFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseFolder.Location = new System.Drawing.Point(516, 66);
            this.btnBrowseFolder.Name = "btnBrowseFolder";
            this.btnBrowseFolder.Size = new System.Drawing.Size(104, 25);
            this.btnBrowseFolder.TabIndex = 4;
            this.btnBrowseFolder.Text = "Обзор…";
            this.btnBrowseFolder.UseVisualStyleBackColor = true;
            this.btnBrowseFolder.Click += new System.EventHandler(this.btnBrowseFolder_Click);
            //
            // labelQueue
            //
            this.labelQueue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelQueue.Location = new System.Drawing.Point(18, 103);
            this.labelQueue.Name = "labelQueue";
            this.labelQueue.Size = new System.Drawing.Size(602, 36);
            this.labelQueue.TabIndex = 5;
            this.labelQueue.Text = "Серийный номер продукта Bridge из этого акта (подсказки при вводе по списку из БД).";
            //
            // lblSerial
            //
            this.lblSerial.AutoSize = true;
            this.lblSerial.Location = new System.Drawing.Point(18, 148);
            this.lblSerial.Name = "lblSerial";
            this.lblSerial.Size = new System.Drawing.Size(105, 15);
            this.lblSerial.TabIndex = 6;
            this.lblSerial.Text = "Серийный номер:";
            //
            // txtSerial
            //
            this.txtSerial.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSerial.Location = new System.Drawing.Point(130, 145);
            this.txtSerial.Name = "txtSerial";
            this.txtSerial.Size = new System.Drawing.Size(490, 23);
            this.txtSerial.TabIndex = 7;
            //
            // btnSaveLogs
            //
            this.btnSaveLogs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveLogs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSaveLogs.Location = new System.Drawing.Point(18, 448);
            this.btnSaveLogs.Name = "btnSaveLogs";
            this.btnSaveLogs.Size = new System.Drawing.Size(602, 40);
            this.btnSaveLogs.TabIndex = 8;
            this.btnSaveLogs.Text = "Сохранить логи";
            this.btnSaveLogs.UseVisualStyleBackColor = true;
            this.btnSaveLogs.Click += new System.EventHandler(this.btnSaveLogs_Click);
            //
            // BridgeLogForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(644, 506);
            this.Controls.Add(this.btnSaveLogs);
            this.Controls.Add(this.txtSerial);
            this.Controls.Add(this.lblSerial);
            this.Controls.Add(this.labelQueue);
            this.Controls.Add(this.btnBrowseFolder);
            this.Controls.Add(this.txtReportsPath);
            this.Controls.Add(this.labelPath);
            this.Controls.Add(this.lblEmployee);
            this.Controls.Add(this.lblAct);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(560, 420);
            this.Name = "BridgeLogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Тестирование Bridge — отчёт Bridge";
            this.Load += new System.EventHandler(this.BridgeLogForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblAct;
        private System.Windows.Forms.Label lblEmployee;
        private System.Windows.Forms.Label labelPath;
        private System.Windows.Forms.TextBox txtReportsPath;
        private System.Windows.Forms.Button btnBrowseFolder;
        private System.Windows.Forms.Label labelQueue;
        private System.Windows.Forms.Label lblSerial;
        private System.Windows.Forms.TextBox txtSerial;
        private System.Windows.Forms.Button btnSaveLogs;
    }
}
