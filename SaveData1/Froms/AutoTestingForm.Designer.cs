namespace SaveData1.Froms
{
    partial class AutoTestingForm
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
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPoletniki = new System.Windows.Forms.TabPage();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.pnlRows = new System.Windows.Forms.FlowLayoutPanel();
            this.lblProductsWithErrors = new System.Windows.Forms.Label();
            this.lstProductsWithErrors = new System.Windows.Forms.ListBox();
            this.pnlPoletLog = new System.Windows.Forms.Panel();
            this.txtLogs = new System.Windows.Forms.TextBox();
            this.lblLogs = new System.Windows.Forms.Label();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblFolder = new System.Windows.Forms.Label();
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblFioAct = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnAddRow = new System.Windows.Forms.Button();
            this.lblMainStatus = new System.Windows.Forms.Label();
            this.tabCrossPlate = new System.Windows.Forms.TabPage();
            this.pnlCrossHost = new System.Windows.Forms.Panel();
            this.tabMain.SuspendLayout();
            this.tabPoletniki.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.pnlPoletLog.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.tabCrossPlate.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabPoletniki);
            this.tabMain.Controls.Add(this.tabCrossPlate);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(677, 541);
            this.tabMain.TabIndex = 0;
            // 
            // tabPoletniki
            // 
            this.tabPoletniki.Controls.Add(this.splitContainer);
            this.tabPoletniki.Controls.Add(this.pnlPoletLog);
            this.tabPoletniki.Controls.Add(this.pnlTop);
            this.tabPoletniki.Location = new System.Drawing.Point(4, 22);
            this.tabPoletniki.Name = "tabPoletniki";
            this.tabPoletniki.Padding = new System.Windows.Forms.Padding(3);
            this.tabPoletniki.Size = new System.Drawing.Size(669, 515);
            this.tabPoletniki.TabIndex = 0;
            this.tabPoletniki.Text = "Полетники (USB)";
            this.tabPoletniki.UseVisualStyleBackColor = true;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(3, 78);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.pnlRows);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.lblProductsWithErrors);
            this.splitContainer.Panel2.Controls.Add(this.lstProductsWithErrors);
            this.splitContainer.Size = new System.Drawing.Size(663, 334);
            this.splitContainer.SplitterDistance = 200;
            this.splitContainer.TabIndex = 2;
            // 
            // pnlRows
            // 
            this.pnlRows.AutoScroll = true;
            this.pnlRows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRows.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlRows.Location = new System.Drawing.Point(0, 0);
            this.pnlRows.Margin = new System.Windows.Forms.Padding(2);
            this.pnlRows.Name = "pnlRows";
            this.pnlRows.Size = new System.Drawing.Size(663, 200);
            this.pnlRows.TabIndex = 0;
            this.pnlRows.WrapContents = false;
            // 
            // lblProductsWithErrors
            // 
            this.lblProductsWithErrors.AutoSize = true;
            this.lblProductsWithErrors.Location = new System.Drawing.Point(3, 5);
            this.lblProductsWithErrors.Name = "lblProductsWithErrors";
            this.lblProductsWithErrors.Size = new System.Drawing.Size(246, 13);
            this.lblProductsWithErrors.TabIndex = 0;
            this.lblProductsWithErrors.Text = "Продукты с ошибками (незаполненный Result):";
            // 
            // lstProductsWithErrors
            // 
            this.lstProductsWithErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstProductsWithErrors.FormattingEnabled = true;
            this.lstProductsWithErrors.Location = new System.Drawing.Point(3, 25);
            this.lstProductsWithErrors.Name = "lstProductsWithErrors";
            this.lstProductsWithErrors.Size = new System.Drawing.Size(657, 95);
            this.lstProductsWithErrors.TabIndex = 1;
            this.lstProductsWithErrors.DoubleClick += new System.EventHandler(this.lstProductsWithErrors_DoubleClick);
            // 
            // pnlPoletLog
            // 
            this.pnlPoletLog.Controls.Add(this.txtLogs);
            this.pnlPoletLog.Controls.Add(this.lblLogs);
            this.pnlPoletLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlPoletLog.Location = new System.Drawing.Point(3, 412);
            this.pnlPoletLog.Name = "pnlPoletLog";
            this.pnlPoletLog.Size = new System.Drawing.Size(663, 100);
            this.pnlPoletLog.TabIndex = 1;
            // 
            // txtLogs
            // 
            this.txtLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogs.Location = new System.Drawing.Point(5, 18);
            this.txtLogs.Multiline = true;
            this.txtLogs.Name = "txtLogs";
            this.txtLogs.ReadOnly = true;
            this.txtLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogs.Size = new System.Drawing.Size(655, 79);
            this.txtLogs.TabIndex = 1;
            // 
            // lblLogs
            // 
            this.lblLogs.AutoSize = true;
            this.lblLogs.Location = new System.Drawing.Point(5, 3);
            this.lblLogs.Name = "lblLogs";
            this.lblLogs.Size = new System.Drawing.Size(29, 13);
            this.lblLogs.TabIndex = 0;
            this.lblLogs.Text = "Лог:";
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.lblFolder);
            this.pnlTop.Controls.Add(this.txtFolderPath);
            this.pnlTop.Controls.Add(this.btnBrowse);
            this.pnlTop.Controls.Add(this.lblFioAct);
            this.pnlTop.Controls.Add(this.btnStart);
            this.pnlTop.Controls.Add(this.btnStop);
            this.pnlTop.Controls.Add(this.btnAddRow);
            this.pnlTop.Controls.Add(this.lblMainStatus);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(3, 3);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(663, 75);
            this.pnlTop.TabIndex = 0;
            // 
            // lblFolder
            // 
            this.lblFolder.AutoSize = true;
            this.lblFolder.Location = new System.Drawing.Point(9, 12);
            this.lblFolder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblFolder.Name = "lblFolder";
            this.lblFolder.Size = new System.Drawing.Size(104, 13);
            this.lblFolder.TabIndex = 0;
            this.lblFolder.Text = "Папка сохранения:";
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Location = new System.Drawing.Point(111, 10);
            this.txtFolderPath.Margin = new System.Windows.Forms.Padding(2);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.Size = new System.Drawing.Size(264, 20);
            this.txtFolderPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(378, 9);
            this.btnBrowse.Margin = new System.Windows.Forms.Padding(2);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(56, 20);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Обзор...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // lblFioAct
            // 
            this.lblFioAct.AutoSize = true;
            this.lblFioAct.Location = new System.Drawing.Point(437, 12);
            this.lblFioAct.Name = "lblFioAct";
            this.lblFioAct.Size = new System.Drawing.Size(0, 13);
            this.lblFioAct.TabIndex = 8;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(9, 41);
            this.btnStart.Margin = new System.Windows.Forms.Padding(2);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 28);
            this.btnStart.TabIndex = 3;
            this.btnStart.Text = "Старт";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(88, 41);
            this.btnStop.Margin = new System.Windows.Forms.Padding(2);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 28);
            this.btnStop.TabIndex = 4;
            this.btnStop.Text = "Стоп";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnAddRow
            // 
            this.btnAddRow.Location = new System.Drawing.Point(176, 41);
            this.btnAddRow.Margin = new System.Windows.Forms.Padding(2);
            this.btnAddRow.Name = "btnAddRow";
            this.btnAddRow.Size = new System.Drawing.Size(112, 28);
            this.btnAddRow.TabIndex = 5;
            this.btnAddRow.Text = "Добавить стенд";
            this.btnAddRow.UseVisualStyleBackColor = true;
            this.btnAddRow.Click += new System.EventHandler(this.btnAddRow_Click);
            // 
            // lblMainStatus
            // 
            this.lblMainStatus.AutoSize = true;
            this.lblMainStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblMainStatus.ForeColor = System.Drawing.Color.Red;
            this.lblMainStatus.Location = new System.Drawing.Point(295, 47);
            this.lblMainStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblMainStatus.Name = "lblMainStatus";
            this.lblMainStatus.Size = new System.Drawing.Size(150, 15);
            this.lblMainStatus.TabIndex = 6;
            this.lblMainStatus.Text = "Статус: Остановлено";
            // 
            // tabCrossPlate
            // 
            this.tabCrossPlate.Controls.Add(this.pnlCrossHost);
            this.tabCrossPlate.Location = new System.Drawing.Point(4, 22);
            this.tabCrossPlate.Name = "tabCrossPlate";
            this.tabCrossPlate.Padding = new System.Windows.Forms.Padding(3);
            this.tabCrossPlate.Size = new System.Drawing.Size(669, 515);
            this.tabCrossPlate.TabIndex = 1;
            this.tabCrossPlate.Text = "Тестирование кросс-плат";
            this.tabCrossPlate.UseVisualStyleBackColor = true;
            // 
            // pnlCrossHost
            // 
            this.pnlCrossHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCrossHost.Location = new System.Drawing.Point(3, 3);
            this.pnlCrossHost.Name = "pnlCrossHost";
            this.pnlCrossHost.Size = new System.Drawing.Size(663, 509);
            this.pnlCrossHost.TabIndex = 0;
            // 
            // AutoTestingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(677, 541);
            this.Controls.Add(this.tabMain);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "AutoTestingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Тестирование";
            this.tabMain.ResumeLayout(false);
            this.tabPoletniki.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.pnlPoletLog.ResumeLayout(false);
            this.pnlPoletLog.PerformLayout();
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.tabCrossPlate.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabPoletniki;
        private System.Windows.Forms.TabPage tabCrossPlate;
        private System.Windows.Forms.Panel pnlCrossHost;
        private System.Windows.Forms.Panel pnlPoletLog;
        private System.Windows.Forms.Label lblLogs;
        private System.Windows.Forms.TextBox txtLogs;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Label lblProductsWithErrors;
        private System.Windows.Forms.ListBox lstProductsWithErrors;
        private System.Windows.Forms.Label lblFioAct;
        private System.Windows.Forms.Label lblFolder;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnAddRow;
        private System.Windows.Forms.FlowLayoutPanel pnlRows;
        private System.Windows.Forms.Label lblMainStatus;
    }
}
