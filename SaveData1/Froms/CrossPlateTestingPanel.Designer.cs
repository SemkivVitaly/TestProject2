namespace SaveData1.Froms
{
    partial class CrossPlateTestingPanel
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabStands = new System.Windows.Forms.TabPage();
            this.panelScrollStands = new System.Windows.Forms.Panel();
            this.groupLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.groupPaths = new System.Windows.Forms.GroupBox();
            this.chkSkipConnectionCheck = new System.Windows.Forms.CheckBox();
            this.chkMonitoringMode = new System.Windows.Forms.CheckBox();
            this.btnScriptGenerator = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.txtExcelOutputFolder = new System.Windows.Forms.TextBox();
            this.lblExcelFolder = new System.Windows.Forms.Label();
            this.btnBrowseExcelFolder = new System.Windows.Forms.Button();
            this.lblDronePort = new System.Windows.Forms.Label();
            this.numDronePort = new System.Windows.Forms.NumericUpDown();
            this.txtDronePing = new System.Windows.Forms.TextBox();
            this.lblDronePing = new System.Windows.Forms.Label();
            this.numTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.btnBrowseScript = new System.Windows.Forms.Button();
            this.txtScriptPath = new System.Windows.Forms.TextBox();
            this.lblScript = new System.Windows.Forms.Label();
            this.btnBrowseMissionPlanner = new System.Windows.Forms.Button();
            this.txtMissionPlannerPath = new System.Windows.Forms.TextBox();
            this.lblMissionPlanner = new System.Windows.Forms.Label();
            this.panelStands = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnAddStand = new System.Windows.Forms.Button();
            this.scrollStands = new System.Windows.Forms.FlowLayoutPanel();
            this.tabDelays = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.tabStands.SuspendLayout();
            this.panelScrollStands.SuspendLayout();
            this.groupLog.SuspendLayout();
            this.groupPaths.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDronePort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit();
            this.panelStands.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabStands);
            this.tabControl.Controls.Add(this.tabDelays);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(740, 765);
            this.tabControl.TabIndex = 0;
            // 
            // tabStands
            // 
            this.tabStands.Controls.Add(this.panelScrollStands);
            this.tabStands.Location = new System.Drawing.Point(4, 22);
            this.tabStands.Name = "tabStands";
            this.tabStands.Padding = new System.Windows.Forms.Padding(3);
            this.tabStands.Size = new System.Drawing.Size(732, 739);
            this.tabStands.TabIndex = 0;
            this.tabStands.Text = "Стенды";
            this.tabStands.UseVisualStyleBackColor = true;
            // 
            // panelScrollStands
            // 
            this.panelScrollStands.AutoScroll = true;
            this.panelScrollStands.AutoScrollMinSize = new System.Drawing.Size(640, 680);
            this.panelScrollStands.Controls.Add(this.groupLog);
            this.panelScrollStands.Controls.Add(this.groupPaths);
            this.panelScrollStands.Controls.Add(this.panelStands);
            this.panelScrollStands.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelScrollStands.Location = new System.Drawing.Point(3, 3);
            this.panelScrollStands.Name = "panelScrollStands";
            this.panelScrollStands.Size = new System.Drawing.Size(726, 733);
            this.panelScrollStands.TabIndex = 0;
            // 
            // groupLog
            // 
            this.groupLog.Controls.Add(this.txtLog);
            this.groupLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupLog.Location = new System.Drawing.Point(0, 680);
            this.groupLog.Name = "groupLog";
            this.groupLog.Size = new System.Drawing.Size(709, 180);
            this.groupLog.TabIndex = 2;
            this.groupLog.TabStop = false;
            this.groupLog.Text = "Лог";
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Location = new System.Drawing.Point(3, 16);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(703, 161);
            this.txtLog.TabIndex = 0;
            // 
            // groupPaths
            // 
            this.groupPaths.Controls.Add(this.chkSkipConnectionCheck);
            this.groupPaths.Controls.Add(this.chkMonitoringMode);
            this.groupPaths.Controls.Add(this.btnScriptGenerator);
            this.groupPaths.Controls.Add(this.btnStop);
            this.groupPaths.Controls.Add(this.btnStart);
            this.groupPaths.Controls.Add(this.txtExcelOutputFolder);
            this.groupPaths.Controls.Add(this.lblExcelFolder);
            this.groupPaths.Controls.Add(this.btnBrowseExcelFolder);
            this.groupPaths.Controls.Add(this.lblDronePort);
            this.groupPaths.Controls.Add(this.numDronePort);
            this.groupPaths.Controls.Add(this.txtDronePing);
            this.groupPaths.Controls.Add(this.lblDronePing);
            this.groupPaths.Controls.Add(this.numTimeout);
            this.groupPaths.Controls.Add(this.lblTimeout);
            this.groupPaths.Controls.Add(this.btnBrowseScript);
            this.groupPaths.Controls.Add(this.txtScriptPath);
            this.groupPaths.Controls.Add(this.lblScript);
            this.groupPaths.Controls.Add(this.btnBrowseMissionPlanner);
            this.groupPaths.Controls.Add(this.txtMissionPlannerPath);
            this.groupPaths.Controls.Add(this.lblMissionPlanner);
            this.groupPaths.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupPaths.Location = new System.Drawing.Point(0, 230);
            this.groupPaths.Name = "groupPaths";
            this.groupPaths.Size = new System.Drawing.Size(709, 258);
            this.groupPaths.TabIndex = 1;
            this.groupPaths.TabStop = false;
            this.groupPaths.Text = "Пути и настройки";
            // 
            // chkSkipConnectionCheck
            // 
            this.chkSkipConnectionCheck.AutoSize = true;
            this.chkSkipConnectionCheck.Location = new System.Drawing.Point(140, 152);
            this.chkSkipConnectionCheck.Name = "chkSkipConnectionCheck";
            this.chkSkipConnectionCheck.Size = new System.Drawing.Size(322, 17);
            this.chkSkipConnectionCheck.TabIndex = 21;
            this.chkSkipConnectionCheck.Text = "Тесты без проверки MAVLink (Ping/UDP для информации)";
            this.chkSkipConnectionCheck.UseVisualStyleBackColor = true;
            // 
            // chkMonitoringMode
            // 
            this.chkMonitoringMode.AutoSize = true;
            this.chkMonitoringMode.Location = new System.Drawing.Point(10, 152);
            this.chkMonitoringMode.Name = "chkMonitoringMode";
            this.chkMonitoringMode.Size = new System.Drawing.Size(119, 17);
            this.chkMonitoringMode.TabIndex = 12;
            this.chkMonitoringMode.Text = "Мониторинг сетей";
            this.chkMonitoringMode.UseVisualStyleBackColor = true;
            // 
            // btnScriptGenerator
            // 
            this.btnScriptGenerator.Location = new System.Drawing.Point(261, 210);
            this.btnScriptGenerator.Name = "btnScriptGenerator";
            this.btnScriptGenerator.Size = new System.Drawing.Size(140, 32);
            this.btnScriptGenerator.TabIndex = 5;
            this.btnScriptGenerator.Text = "Генератор скриптов";
            this.btnScriptGenerator.UseVisualStyleBackColor = true;
            this.btnScriptGenerator.Click += new System.EventHandler(this.btnScriptGenerator_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(135, 208);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(120, 32);
            this.btnStop.TabIndex = 4;
            this.btnStop.Text = "Стоп";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.btnStart.Location = new System.Drawing.Point(9, 210);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(120, 32);
            this.btnStart.TabIndex = 3;
            this.btnStart.Text = "Старт";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // txtExcelOutputFolder
            // 
            this.txtExcelOutputFolder.Location = new System.Drawing.Point(140, 178);
            this.txtExcelOutputFolder.Name = "txtExcelOutputFolder";
            this.txtExcelOutputFolder.Size = new System.Drawing.Size(350, 20);
            this.txtExcelOutputFolder.TabIndex = 19;
            // 
            // lblExcelFolder
            // 
            this.lblExcelFolder.AutoSize = true;
            this.lblExcelFolder.Location = new System.Drawing.Point(10, 181);
            this.lblExcelFolder.Name = "lblExcelFolder";
            this.lblExcelFolder.Size = new System.Drawing.Size(134, 13);
            this.lblExcelFolder.TabIndex = 18;
            this.lblExcelFolder.Text = "Папка для Excel-отчётов:";
            // 
            // btnBrowseExcelFolder
            // 
            this.btnBrowseExcelFolder.Location = new System.Drawing.Point(506, 176);
            this.btnBrowseExcelFolder.Name = "btnBrowseExcelFolder";
            this.btnBrowseExcelFolder.Size = new System.Drawing.Size(70, 24);
            this.btnBrowseExcelFolder.TabIndex = 20;
            this.btnBrowseExcelFolder.Text = "Обзор...";
            this.btnBrowseExcelFolder.UseVisualStyleBackColor = true;
            this.btnBrowseExcelFolder.Click += new System.EventHandler(this.btnBrowseExcelFolder_Click);
            // 
            // lblDronePort
            // 
            this.lblDronePort.AutoSize = true;
            this.lblDronePort.Location = new System.Drawing.Point(468, 98);
            this.lblDronePort.Name = "lblDronePort";
            this.lblDronePort.Size = new System.Drawing.Size(68, 13);
            this.lblDronePort.TabIndex = 10;
            this.lblDronePort.Text = "Порт дрона:";
            // 
            // numDronePort
            // 
            this.numDronePort.Location = new System.Drawing.Point(542, 96);
            this.numDronePort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numDronePort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numDronePort.Name = "numDronePort";
            this.numDronePort.Size = new System.Drawing.Size(70, 20);
            this.numDronePort.TabIndex = 11;
            this.numDronePort.Value = new decimal(new int[] {
            14550,
            0,
            0,
            0});
            // 
            // txtDronePing
            // 
            this.txtDronePing.Location = new System.Drawing.Point(282, 95);
            this.txtDronePing.Name = "txtDronePing";
            this.txtDronePing.Size = new System.Drawing.Size(180, 20);
            this.txtDronePing.TabIndex = 7;
            this.txtDronePing.Text = "192.168.4.1;192.168.1.1";
            // 
            // lblDronePing
            // 
            this.lblDronePing.AutoSize = true;
            this.lblDronePing.Location = new System.Drawing.Point(10, 98);
            this.lblDronePing.Name = "lblDronePing";
            this.lblDronePing.Size = new System.Drawing.Size(267, 13);
            this.lblDronePing.TabIndex = 6;
            this.lblDronePing.Text = "Проверка подключения Mission Planner (IP, через ;):";
            // 
            // numTimeout
            // 
            this.numTimeout.Location = new System.Drawing.Point(296, 125);
            this.numTimeout.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numTimeout.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numTimeout.Name = "numTimeout";
            this.numTimeout.Size = new System.Drawing.Size(60, 20);
            this.numTimeout.TabIndex = 9;
            this.numTimeout.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Location = new System.Drawing.Point(10, 128);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(253, 13);
            this.lblTimeout.TabIndex = 8;
            this.lblTimeout.Text = "Ожидание подключения Mission Planner (секунд):";
            // 
            // btnBrowseScript
            // 
            this.btnBrowseScript.Location = new System.Drawing.Point(506, 65);
            this.btnBrowseScript.Name = "btnBrowseScript";
            this.btnBrowseScript.Size = new System.Drawing.Size(70, 24);
            this.btnBrowseScript.TabIndex = 5;
            this.btnBrowseScript.Text = "Обзор...";
            this.btnBrowseScript.UseVisualStyleBackColor = true;
            this.btnBrowseScript.Click += new System.EventHandler(this.btnBrowseScript_Click);
            // 
            // txtScriptPath
            // 
            this.txtScriptPath.Location = new System.Drawing.Point(140, 69);
            this.txtScriptPath.Name = "txtScriptPath";
            this.txtScriptPath.Size = new System.Drawing.Size(350, 20);
            this.txtScriptPath.TabIndex = 4;
            // 
            // lblScript
            // 
            this.lblScript.AutoSize = true;
            this.lblScript.Location = new System.Drawing.Point(10, 72);
            this.lblScript.Name = "lblScript";
            this.lblScript.Size = new System.Drawing.Size(111, 13);
            this.lblScript.TabIndex = 3;
            this.lblScript.Text = "Скрипт для запуска:";
            // 
            // btnBrowseMissionPlanner
            // 
            this.btnBrowseMissionPlanner.Location = new System.Drawing.Point(506, 19);
            this.btnBrowseMissionPlanner.Name = "btnBrowseMissionPlanner";
            this.btnBrowseMissionPlanner.Size = new System.Drawing.Size(70, 24);
            this.btnBrowseMissionPlanner.TabIndex = 2;
            this.btnBrowseMissionPlanner.Text = "Обзор...";
            this.btnBrowseMissionPlanner.UseVisualStyleBackColor = true;
            this.btnBrowseMissionPlanner.Click += new System.EventHandler(this.btnBrowseMissionPlanner_Click);
            // 
            // txtMissionPlannerPath
            // 
            this.txtMissionPlannerPath.Location = new System.Drawing.Point(140, 22);
            this.txtMissionPlannerPath.Name = "txtMissionPlannerPath";
            this.txtMissionPlannerPath.Size = new System.Drawing.Size(350, 20);
            this.txtMissionPlannerPath.TabIndex = 1;
            // 
            // lblMissionPlanner
            // 
            this.lblMissionPlanner.AutoSize = true;
            this.lblMissionPlanner.Location = new System.Drawing.Point(10, 25);
            this.lblMissionPlanner.Name = "lblMissionPlanner";
            this.lblMissionPlanner.Size = new System.Drawing.Size(110, 13);
            this.lblMissionPlanner.TabIndex = 0;
            this.lblMissionPlanner.Text = "Mission Planner (exe):";
            // 
            // panelStands
            // 
            this.panelStands.AutoScroll = true;
            this.panelStands.Controls.Add(this.scrollStands);
            this.panelStands.Controls.Add(this.panel1);
            this.panelStands.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelStands.Location = new System.Drawing.Point(0, 0);
            this.panelStands.Name = "panelStands";
            this.panelStands.Size = new System.Drawing.Size(709, 230);
            this.panelStands.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnAddStand);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(709, 39);
            this.panel1.TabIndex = 2;
            // 
            // btnAddStand
            // 
            this.btnAddStand.Location = new System.Drawing.Point(3, 3);
            this.btnAddStand.Name = "btnAddStand";
            this.btnAddStand.Size = new System.Drawing.Size(120, 28);
            this.btnAddStand.TabIndex = 1;
            this.btnAddStand.Text = "Добавить стенд";
            this.btnAddStand.UseVisualStyleBackColor = true;
            this.btnAddStand.Click += new System.EventHandler(this.btnAddStand_Click);
            // 
            // scrollStands
            // 
            this.scrollStands.AutoScroll = true;
            this.scrollStands.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scrollStands.Location = new System.Drawing.Point(0, 39);
            this.scrollStands.Name = "scrollStands";
            this.scrollStands.Size = new System.Drawing.Size(709, 191);
            this.scrollStands.TabIndex = 0;
            // 
            // tabDelays
            // 
            this.tabDelays.Location = new System.Drawing.Point(4, 22);
            this.tabDelays.Name = "tabDelays";
            this.tabDelays.Padding = new System.Windows.Forms.Padding(3);
            this.tabDelays.Size = new System.Drawing.Size(732, 739);
            this.tabDelays.TabIndex = 1;
            this.tabDelays.Text = "Задержки";
            this.tabDelays.UseVisualStyleBackColor = true;
            // 
            // CrossPlateTestingPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl);
            this.Name = "CrossPlateTestingPanel";
            this.Size = new System.Drawing.Size(740, 765);
            this.Load += new System.EventHandler(this.CrossPlateTestingPanel_Load);
            this.tabControl.ResumeLayout(false);
            this.tabStands.ResumeLayout(false);
            this.panelScrollStands.ResumeLayout(false);
            this.groupLog.ResumeLayout(false);
            this.groupLog.PerformLayout();
            this.groupPaths.ResumeLayout(false);
            this.groupPaths.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDronePort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit();
            this.panelStands.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabStands;
        private System.Windows.Forms.Panel panelScrollStands;
        private System.Windows.Forms.TabPage tabDelays;
        private System.Windows.Forms.Panel panelStands;
        private System.Windows.Forms.Button btnAddStand;
        private System.Windows.Forms.FlowLayoutPanel scrollStands;
        private System.Windows.Forms.GroupBox groupPaths;
        private System.Windows.Forms.Label lblMissionPlanner;
        private System.Windows.Forms.TextBox txtMissionPlannerPath;
        private System.Windows.Forms.Button btnBrowseMissionPlanner;
        private System.Windows.Forms.Label lblScript;
        private System.Windows.Forms.TextBox txtScriptPath;
        private System.Windows.Forms.Button btnBrowseScript;
        private System.Windows.Forms.Label lblDronePing;
        private System.Windows.Forms.TextBox txtDronePing;
        private System.Windows.Forms.Label lblDronePort;
        private System.Windows.Forms.NumericUpDown numDronePort;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.NumericUpDown numTimeout;
        private System.Windows.Forms.GroupBox groupLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnScriptGenerator;
        private System.Windows.Forms.CheckBox chkSkipConnectionCheck;
        private System.Windows.Forms.CheckBox chkMonitoringMode;
        private System.Windows.Forms.TextBox txtExcelOutputFolder;
        private System.Windows.Forms.Label lblExcelFolder;
        private System.Windows.Forms.Button btnBrowseExcelFolder;
        private System.Windows.Forms.Panel panel1;
    }
}
