namespace SaveData1.Froms
{
    partial class QualityControlForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.TextBox txtScanBuffer;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgv = new System.Windows.Forms.DataGridView();
            this.txtScanBuffer = new System.Windows.Forms.TextBox();
            this.lblHint = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            //
            // dgv
            //
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Location = new System.Drawing.Point(12, 12);
            this.dgv.Name = "dgv";
            this.dgv.Size = new System.Drawing.Size(560, 320);
            this.dgv.TabIndex = 0;
            //
            // lblHint
            //
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblHint.AutoSize = true;
            this.lblHint.Location = new System.Drawing.Point(12, 342);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(380, 13);
            this.lblHint.TabIndex = 1;
            this.lblHint.Text = "Сканируйте QR (фокус в поле ниже), Enter — проверка серийного номера.";
            //
            // txtScanBuffer
            //
            this.txtScanBuffer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtScanBuffer.Location = new System.Drawing.Point(12, 362);
            this.txtScanBuffer.Name = "txtScanBuffer";
            this.txtScanBuffer.Size = new System.Drawing.Size(560, 20);
            this.txtScanBuffer.TabIndex = 2;
            this.txtScanBuffer.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtScanBuffer_KeyDown);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 388);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 3;
            //
            // btnSave
            //
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(316, 408);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 28);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(452, 408);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(120, 28);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // QualityControlForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 451);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtScanBuffer);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.dgv);
            this.MinimizeBox = false;
            this.Name = "QualityControlForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Контроль";
            this.Load += new System.EventHandler(this.QualityControlForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
