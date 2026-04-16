namespace SaveData1.Froms
{
    partial class PostTestingShipToWarehouseForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Label lblHint;
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
            this.lblCount = new System.Windows.Forms.Label();
            this.lblHint = new System.Windows.Forms.Label();
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
            this.dgv.Location = new System.Drawing.Point(12, 36);
            this.dgv.Name = "dgv";
            this.dgv.Size = new System.Drawing.Size(560, 300);
            this.dgv.TabIndex = 0;
            //
            // lblCount
            //
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(12, 12);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(0, 13);
            this.lblCount.TabIndex = 1;
            //
            // lblHint
            //
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblHint.AutoSize = true;
            this.lblHint.Location = new System.Drawing.Point(12, 346);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(420, 13);
            this.lblHint.TabIndex = 2;
            this.lblHint.Text = "Продукты с успешным тестом и прошедшие контроль. Сохранить — передать на склад.";
            //
            // btnSave
            //
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(316, 372);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 28);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(452, 372);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(120, 28);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // PostTestingShipToWarehouseForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 412);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.dgv);
            this.MinimizeBox = false;
            this.Name = "PostTestingShipToWarehouseForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Отгрузка на склад";
            this.Load += new System.EventHandler(this.PostTestingShipToWarehouseForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
