namespace SaveData1
{
    partial class EmployeeForm
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
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.tabEmployee = new System.Windows.Forms.TabPage();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lstActs = new System.Windows.Forms.ListBox();
            this.lblActs = new System.Windows.Forms.Label();
            this.dgvProducts = new System.Windows.Forms.DataGridView();
            this.panelPath = new System.Windows.Forms.Panel();
            this.btnBrowseUserPath = new System.Windows.Forms.Button();
            this.txtUserPath = new System.Windows.Forms.TextBox();
            this.lblUserPath = new System.Windows.Forms.Label();
            this.panelActions = new System.Windows.Forms.Panel();
            this.tabControlActions = new System.Windows.Forms.TabControl();
            this.tabPageActionsGeneral = new System.Windows.Forms.TabPage();
            this.btnCreateActFolders = new System.Windows.Forms.Button();
            this.btnSaveChanges = new System.Windows.Forms.Button();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnGenerateQrEmployee = new System.Windows.Forms.Button();
            this.btnNonConformity = new System.Windows.Forms.Button();
            this.tabPageActionsAdmin = new System.Windows.Forms.TabPage();
            this.btnChangeStatus = new System.Windows.Forms.Button();
            this.tabPageActionsTester = new System.Windows.Forms.TabPage();
            this.btnBridgeTesting = new System.Windows.Forms.Button();
            this.btnCrossPlateTesting = new System.Windows.Forms.Button();
            this.btnAdvancedTesting = new System.Windows.Forms.Button();
            this.tabControlWork = new System.Windows.Forms.TabControl();
            this.tabPageAssembly = new System.Windows.Forms.TabPage();
            this.tabPageTesting = new System.Windows.Forms.TabPage();
            this.tabPageInspection = new System.Windows.Forms.TabPage();
            this.panelFilters = new System.Windows.Forms.Panel();
            this.btnResetFilter = new System.Windows.Forms.Button();
            this.btnApplyFilter = new System.Windows.Forms.Button();
            this.chkTimeFilter = new System.Windows.Forms.CheckBox();
            this.dtpTimeTo = new System.Windows.Forms.DateTimePicker();
            this.lblTimeTo = new System.Windows.Forms.Label();
            this.dtpTimeFrom = new System.Windows.Forms.DateTimePicker();
            this.lblTimeFrom = new System.Windows.Forms.Label();
            this.chkDateFilter = new System.Windows.Forms.CheckBox();
            this.dtpDateTo = new System.Windows.Forms.DateTimePicker();
            this.lblDateTo = new System.Windows.Forms.Label();
            this.dtpDateFrom = new System.Windows.Forms.DateTimePicker();
            this.lblDateFrom = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.tabAdmin = new System.Windows.Forms.TabPage();
            this.adminTabControl = new System.Windows.Forms.TabControl();
            this.tabAdminUsers = new System.Windows.Forms.TabPage();
            this.grpUsers = new System.Windows.Forms.GroupBox();
            this.dgvUsers = new System.Windows.Forms.DataGridView();
            this.panelUserActions = new System.Windows.Forms.Panel();
            this.btnAddUser = new System.Windows.Forms.Button();
            this.btnEditUser = new System.Windows.Forms.Button();
            this.btnDeleteUser = new System.Windows.Forms.Button();
            this.tabAdminNoAct = new System.Windows.Forms.TabPage();
            this.grpNoAct = new System.Windows.Forms.GroupBox();
            this.dgvNoActProducts = new System.Windows.Forms.DataGridView();
            this.panelNoActTop = new System.Windows.Forms.Panel();
            this.lblAdminNewCategory = new System.Windows.Forms.Label();
            this.txtAdminNewCategory = new System.Windows.Forms.TextBox();
            this.lblAdminCountry = new System.Windows.Forms.Label();
            this.cmbAdminCountry = new System.Windows.Forms.ComboBox();
            this.btnAdminAddCategory = new System.Windows.Forms.Button();
            this.lblAdminSerial = new System.Windows.Forms.Label();
            this.txtAdminSerial = new System.Windows.Forms.TextBox();
            this.cmbAdminProductType = new System.Windows.Forms.ComboBox();
            this.btnAdminAddProduct = new System.Windows.Forms.Button();
            this.tabAdminActs = new System.Windows.Forms.TabPage();
            this.btnAdminAssign = new System.Windows.Forms.Button();
            this.btnAdminGenerateQr = new System.Windows.Forms.Button();
            this.dgvAdminUnassigned = new System.Windows.Forms.DataGridView();
            this.lblAdminUnassigned = new System.Windows.Forms.Label();
            this.lblActExplain = new System.Windows.Forms.Label();
            this.btnBrowsePath = new System.Windows.Forms.Button();
            this.txtActPath = new System.Windows.Forms.TextBox();
            this.lblActPath = new System.Windows.Forms.Label();
            this.lblAdminSelectAct = new System.Windows.Forms.Label();
            this.cmbAdminActs = new System.Windows.Forms.ComboBox();
            this.grpAdminAct = new System.Windows.Forms.GroupBox();
            this.lblAdminActNumber = new System.Windows.Forms.Label();
            this.txtAdminActNumber = new System.Windows.Forms.TextBox();
            this.btnAdminCreateAct = new System.Windows.Forms.Button();
            this.tabAdminStatistics = new System.Windows.Forms.TabPage();
            this.btnRefreshStats = new System.Windows.Forms.Button();
            this.grpStatsByDefect = new System.Windows.Forms.GroupBox();
            this.dgvStatsByDefect = new System.Windows.Forms.DataGridView();
            this.grpStatsByStage = new System.Windows.Forms.GroupBox();
            this.dgvStatsByStage = new System.Windows.Forms.DataGridView();
            this.splitAdmin = new System.Windows.Forms.SplitContainer();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.mainTabControl.SuspendLayout();
            this.tabEmployee.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProducts)).BeginInit();
            this.panelPath.SuspendLayout();
            this.panelActions.SuspendLayout();
            this.tabControlActions.SuspendLayout();
            this.tabPageActionsGeneral.SuspendLayout();
            this.tabPageActionsAdmin.SuspendLayout();
            this.tabPageActionsTester.SuspendLayout();
            this.tabControlWork.SuspendLayout();
            this.panelFilters.SuspendLayout();
            this.tabAdmin.SuspendLayout();
            this.adminTabControl.SuspendLayout();
            this.tabAdminUsers.SuspendLayout();
            this.grpUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).BeginInit();
            this.panelUserActions.SuspendLayout();
            this.tabAdminNoAct.SuspendLayout();
            this.grpNoAct.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNoActProducts)).BeginInit();
            this.panelNoActTop.SuspendLayout();
            this.tabAdminActs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAdminUnassigned)).BeginInit();
            this.grpAdminAct.SuspendLayout();
            this.tabAdminStatistics.SuspendLayout();
            this.grpStatsByDefect.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatsByDefect)).BeginInit();
            this.grpStatsByStage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatsByStage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitAdmin)).BeginInit();
            this.splitAdmin.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTabControl
            // 
            this.mainTabControl.Controls.Add(this.tabEmployee);
            this.mainTabControl.Controls.Add(this.tabAdmin);
            this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabControl.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.mainTabControl.Location = new System.Drawing.Point(0, 35);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(1100, 665);
            this.mainTabControl.TabIndex = 0;
            // 
            // tabEmployee
            // 
            this.tabEmployee.Controls.Add(this.splitContainer);
            this.tabEmployee.Location = new System.Drawing.Point(4, 26);
            this.tabEmployee.Name = "tabEmployee";
            this.tabEmployee.Size = new System.Drawing.Size(1092, 635);
            this.tabEmployee.TabIndex = 0;
            this.tabEmployee.Text = "Работа с продуктами";
            this.tabEmployee.UseVisualStyleBackColor = true;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.lstActs);
            this.splitContainer.Panel1.Controls.Add(this.lblActs);
            this.splitContainer.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.dgvProducts);
            this.splitContainer.Panel2.Controls.Add(this.panelPath);
            this.splitContainer.Panel2.Controls.Add(this.panelActions);
            this.splitContainer.Panel2.Controls.Add(this.tabControlWork);
            this.splitContainer.Panel2.Controls.Add(this.panelFilters);
            this.splitContainer.Size = new System.Drawing.Size(1092, 635);
            this.splitContainer.SplitterDistance = 220;
            this.splitContainer.TabIndex = 0;
            // 
            // lstActs
            // 
            this.lstActs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstActs.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstActs.FormattingEnabled = true;
            this.lstActs.ItemHeight = 17;
            this.lstActs.Location = new System.Drawing.Point(5, 35);
            this.lstActs.Name = "lstActs";
            this.lstActs.Size = new System.Drawing.Size(210, 595);
            this.lstActs.TabIndex = 1;
            this.lstActs.SelectedIndexChanged += new System.EventHandler(this.lstActs_SelectedIndexChanged);
            // 
            // lblActs
            // 
            this.lblActs.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblActs.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblActs.Location = new System.Drawing.Point(5, 5);
            this.lblActs.Name = "lblActs";
            this.lblActs.Size = new System.Drawing.Size(210, 30);
            this.lblActs.TabIndex = 0;
            this.lblActs.Text = "Акты";
            this.lblActs.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dgvProducts
            // 
            this.dgvProducts.AllowUserToAddRows = false;
            this.dgvProducts.AllowUserToDeleteRows = false;
            this.dgvProducts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvProducts.Location = new System.Drawing.Point(0, 128);
            this.dgvProducts.Name = "dgvProducts";
            this.dgvProducts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvProducts.Size = new System.Drawing.Size(868, 389);
            this.dgvProducts.TabIndex = 1;
            this.dgvProducts.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvProducts_CellDoubleClick);
            // 
            // panelPath
            // 
            this.panelPath.Controls.Add(this.btnBrowseUserPath);
            this.panelPath.Controls.Add(this.txtUserPath);
            this.panelPath.Controls.Add(this.lblUserPath);
            this.panelPath.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelPath.Location = new System.Drawing.Point(0, 517);
            this.panelPath.Name = "panelPath";
            this.panelPath.Padding = new System.Windows.Forms.Padding(5);
            this.panelPath.Size = new System.Drawing.Size(868, 35);
            this.panelPath.TabIndex = 3;
            // 
            // btnBrowseUserPath
            // 
            this.btnBrowseUserPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseUserPath.Location = new System.Drawing.Point(822, 4);
            this.btnBrowseUserPath.Name = "btnBrowseUserPath";
            this.btnBrowseUserPath.Size = new System.Drawing.Size(40, 25);
            this.btnBrowseUserPath.TabIndex = 2;
            this.btnBrowseUserPath.Text = "...";
            this.btnBrowseUserPath.UseVisualStyleBackColor = true;
            this.btnBrowseUserPath.Click += new System.EventHandler(this.btnBrowseUserPath_Click);
            // 
            // txtUserPath
            // 
            this.txtUserPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUserPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtUserPath.Location = new System.Drawing.Point(55, 5);
            this.txtUserPath.Name = "txtUserPath";
            this.txtUserPath.Size = new System.Drawing.Size(760, 23);
            this.txtUserPath.TabIndex = 1;
            this.txtUserPath.TextChanged += new System.EventHandler(this.txtUserPath_TextChanged);
            // 
            // lblUserPath
            // 
            this.lblUserPath.AutoSize = true;
            this.lblUserPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblUserPath.Location = new System.Drawing.Point(8, 8);
            this.lblUserPath.Name = "lblUserPath";
            this.lblUserPath.Size = new System.Drawing.Size(36, 15);
            this.lblUserPath.TabIndex = 0;
            this.lblUserPath.Text = "Путь:";
            // 
            // panelActions
            // 
            this.panelActions.Controls.Add(this.tabControlActions);
            this.panelActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelActions.Location = new System.Drawing.Point(0, 552);
            this.panelActions.Name = "panelActions";
            this.panelActions.Padding = new System.Windows.Forms.Padding(5);
            this.panelActions.Size = new System.Drawing.Size(868, 83);
            this.panelActions.TabIndex = 2;
            // 
            // tabControlActions
            // 
            this.tabControlActions.Controls.Add(this.tabPageActionsGeneral);
            this.tabControlActions.Controls.Add(this.tabPageActionsAdmin);
            this.tabControlActions.Controls.Add(this.tabPageActionsTester);
            this.tabControlActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlActions.Location = new System.Drawing.Point(5, 5);
            this.tabControlActions.Name = "tabControlActions";
            this.tabControlActions.SelectedIndex = 0;
            this.tabControlActions.Size = new System.Drawing.Size(858, 73);
            this.tabControlActions.TabIndex = 0;
            // 
            // tabPageActionsGeneral
            // 
            this.tabPageActionsGeneral.Controls.Add(this.btnCreateActFolders);
            this.tabPageActionsGeneral.Controls.Add(this.btnSaveChanges);
            this.tabPageActionsGeneral.Controls.Add(this.btnExportExcel);
            this.tabPageActionsGeneral.Controls.Add(this.btnRefresh);
            this.tabPageActionsGeneral.Controls.Add(this.btnGenerateQrEmployee);
            this.tabPageActionsGeneral.Controls.Add(this.btnNonConformity);
            this.tabPageActionsGeneral.Location = new System.Drawing.Point(4, 26);
            this.tabPageActionsGeneral.Name = "tabPageActionsGeneral";
            this.tabPageActionsGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageActionsGeneral.Size = new System.Drawing.Size(850, 43);
            this.tabPageActionsGeneral.TabIndex = 0;
            this.tabPageActionsGeneral.Text = "Общие";
            this.tabPageActionsGeneral.UseVisualStyleBackColor = true;
            // 
            // btnCreateActFolders
            // 
            this.btnCreateActFolders.Location = new System.Drawing.Point(711, 4);
            this.btnCreateActFolders.Name = "btnCreateActFolders";
            this.btnCreateActFolders.Size = new System.Drawing.Size(135, 28);
            this.btnCreateActFolders.TabIndex = 5;
            this.btnCreateActFolders.Text = "Создать папки акта";
            this.btnCreateActFolders.UseVisualStyleBackColor = true;
            this.btnCreateActFolders.Click += new System.EventHandler(this.btnCreateActFolders_Click);
            // 
            // btnSaveChanges
            // 
            this.btnSaveChanges.Location = new System.Drawing.Point(6, 4);
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Size = new System.Drawing.Size(150, 28);
            this.btnSaveChanges.TabIndex = 0;
            this.btnSaveChanges.Text = "Сохранить изменения";
            this.btnSaveChanges.UseVisualStyleBackColor = true;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Location = new System.Drawing.Point(162, 4);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(130, 28);
            this.btnExportExcel.TabIndex = 1;
            this.btnExportExcel.Text = "Сохранить в Excel";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(298, 4);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(90, 28);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Обновить";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnGenerateQrEmployee
            // 
            this.btnGenerateQrEmployee.Location = new System.Drawing.Point(394, 4);
            this.btnGenerateQrEmployee.Name = "btnGenerateQrEmployee";
            this.btnGenerateQrEmployee.Size = new System.Drawing.Size(140, 28);
            this.btnGenerateQrEmployee.TabIndex = 3;
            this.btnGenerateQrEmployee.Text = "QR-коды для акта";
            this.btnGenerateQrEmployee.UseVisualStyleBackColor = true;
            this.btnGenerateQrEmployee.Click += new System.EventHandler(this.btnGenerateQrEmployee_Click);
            // 
            // btnNonConformity
            // 
            this.btnNonConformity.Location = new System.Drawing.Point(540, 4);
            this.btnNonConformity.Name = "btnNonConformity";
            this.btnNonConformity.Size = new System.Drawing.Size(165, 28);
            this.btnNonConformity.TabIndex = 4;
            this.btnNonConformity.Text = "Ярлык несоответствия";
            this.btnNonConformity.UseVisualStyleBackColor = true;
            this.btnNonConformity.Click += new System.EventHandler(this.btnNonConformity_Click);
            // 
            // tabPageActionsAdmin
            // 
            this.tabPageActionsAdmin.Controls.Add(this.btnChangeStatus);
            this.tabPageActionsAdmin.Location = new System.Drawing.Point(4, 26);
            this.tabPageActionsAdmin.Name = "tabPageActionsAdmin";
            this.tabPageActionsAdmin.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageActionsAdmin.Size = new System.Drawing.Size(850, 43);
            this.tabPageActionsAdmin.TabIndex = 1;
            this.tabPageActionsAdmin.Text = "Администратор";
            this.tabPageActionsAdmin.UseVisualStyleBackColor = true;
            // 
            // btnChangeStatus
            // 
            this.btnChangeStatus.Location = new System.Drawing.Point(6, 4);
            this.btnChangeStatus.Name = "btnChangeStatus";
            this.btnChangeStatus.Size = new System.Drawing.Size(140, 28);
            this.btnChangeStatus.TabIndex = 0;
            this.btnChangeStatus.Text = "Изменить статус";
            this.btnChangeStatus.UseVisualStyleBackColor = true;
            this.btnChangeStatus.Visible = false;
            this.btnChangeStatus.Click += new System.EventHandler(this.btnChangeStatus_Click);
            // 
            // tabPageActionsTester
            // 
            this.tabPageActionsTester.Controls.Add(this.btnBridgeTesting);
            this.tabPageActionsTester.Controls.Add(this.btnCrossPlateTesting);
            this.tabPageActionsTester.Controls.Add(this.btnAdvancedTesting);
            this.tabPageActionsTester.Location = new System.Drawing.Point(4, 26);
            this.tabPageActionsTester.Name = "tabPageActionsTester";
            this.tabPageActionsTester.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageActionsTester.Size = new System.Drawing.Size(850, 43);
            this.tabPageActionsTester.TabIndex = 2;
            this.tabPageActionsTester.Text = "Тестировщик";
            this.tabPageActionsTester.UseVisualStyleBackColor = true;
            // 
            // btnBridgeTesting
            // 
            this.btnBridgeTesting.Location = new System.Drawing.Point(418, 4);
            this.btnBridgeTesting.Name = "btnBridgeTesting";
            this.btnBridgeTesting.Size = new System.Drawing.Size(200, 28);
            this.btnBridgeTesting.TabIndex = 2;
            this.btnBridgeTesting.Text = "Тестирование Bridge";
            this.btnBridgeTesting.UseVisualStyleBackColor = true;
            this.btnBridgeTesting.Click += new System.EventHandler(this.btnBridgeTesting_Click);
            // 
            // btnCrossPlateTesting
            // 
            this.btnCrossPlateTesting.Location = new System.Drawing.Point(212, 4);
            this.btnCrossPlateTesting.Name = "btnCrossPlateTesting";
            this.btnCrossPlateTesting.Size = new System.Drawing.Size(200, 28);
            this.btnCrossPlateTesting.TabIndex = 1;
            this.btnCrossPlateTesting.Text = "Тестирование кросс-плат";
            this.btnCrossPlateTesting.UseVisualStyleBackColor = true;
            this.btnCrossPlateTesting.Click += new System.EventHandler(this.btnCrossPlateTesting_Click);
            // 
            // btnAdvancedTesting
            // 
            this.btnAdvancedTesting.Location = new System.Drawing.Point(6, 4);
            this.btnAdvancedTesting.Name = "btnAdvancedTesting";
            this.btnAdvancedTesting.Size = new System.Drawing.Size(200, 28);
            this.btnAdvancedTesting.TabIndex = 0;
            this.btnAdvancedTesting.Text = "Тестирование полетников";
            this.btnAdvancedTesting.UseVisualStyleBackColor = true;
            this.btnAdvancedTesting.Click += new System.EventHandler(this.btnAdvancedTesting_Click);
            // 
            // tabControlWork
            // 
            this.tabControlWork.Controls.Add(this.tabPageAssembly);
            this.tabControlWork.Controls.Add(this.tabPageTesting);
            this.tabControlWork.Controls.Add(this.tabPageInspection);
            this.tabControlWork.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControlWork.Location = new System.Drawing.Point(0, 100);
            this.tabControlWork.Name = "tabControlWork";
            this.tabControlWork.Padding = new System.Drawing.Point(12, 4);
            this.tabControlWork.SelectedIndex = 0;
            this.tabControlWork.Size = new System.Drawing.Size(868, 28);
            this.tabControlWork.TabIndex = 4;
            this.tabControlWork.SelectedIndexChanged += new System.EventHandler(this.tabControlWork_SelectedIndexChanged);
            // 
            // tabPageAssembly
            // 
            this.tabPageAssembly.Location = new System.Drawing.Point(4, 28);
            this.tabPageAssembly.Name = "tabPageAssembly";
            this.tabPageAssembly.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAssembly.Size = new System.Drawing.Size(860, 0);
            this.tabPageAssembly.TabIndex = 0;
            this.tabPageAssembly.Text = "Сборка";
            this.tabPageAssembly.UseVisualStyleBackColor = true;
            // 
            // tabPageTesting
            // 
            this.tabPageTesting.Location = new System.Drawing.Point(4, 28);
            this.tabPageTesting.Name = "tabPageTesting";
            this.tabPageTesting.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTesting.Size = new System.Drawing.Size(860, 0);
            this.tabPageTesting.TabIndex = 1;
            this.tabPageTesting.Text = "Тестирование";
            this.tabPageTesting.UseVisualStyleBackColor = true;
            // 
            // tabPageInspection
            // 
            this.tabPageInspection.Location = new System.Drawing.Point(4, 28);
            this.tabPageInspection.Name = "tabPageInspection";
            this.tabPageInspection.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageInspection.Size = new System.Drawing.Size(860, 0);
            this.tabPageInspection.TabIndex = 2;
            this.tabPageInspection.Text = "Инспекция";
            this.tabPageInspection.UseVisualStyleBackColor = true;
            // 
            // panelFilters
            // 
            this.panelFilters.Controls.Add(this.btnResetFilter);
            this.panelFilters.Controls.Add(this.btnApplyFilter);
            this.panelFilters.Controls.Add(this.chkTimeFilter);
            this.panelFilters.Controls.Add(this.dtpTimeTo);
            this.panelFilters.Controls.Add(this.lblTimeTo);
            this.panelFilters.Controls.Add(this.dtpTimeFrom);
            this.panelFilters.Controls.Add(this.lblTimeFrom);
            this.panelFilters.Controls.Add(this.chkDateFilter);
            this.panelFilters.Controls.Add(this.dtpDateTo);
            this.panelFilters.Controls.Add(this.lblDateTo);
            this.panelFilters.Controls.Add(this.dtpDateFrom);
            this.panelFilters.Controls.Add(this.lblDateFrom);
            this.panelFilters.Controls.Add(this.txtSearch);
            this.panelFilters.Controls.Add(this.lblSearch);
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Location = new System.Drawing.Point(0, 0);
            this.panelFilters.Name = "panelFilters";
            this.panelFilters.Padding = new System.Windows.Forms.Padding(5);
            this.panelFilters.Size = new System.Drawing.Size(868, 100);
            this.panelFilters.TabIndex = 0;
            // 
            // btnResetFilter
            // 
            this.btnResetFilter.Location = new System.Drawing.Point(380, 67);
            this.btnResetFilter.Name = "btnResetFilter";
            this.btnResetFilter.Size = new System.Drawing.Size(110, 28);
            this.btnResetFilter.TabIndex = 13;
            this.btnResetFilter.Text = "Сбросить";
            this.btnResetFilter.UseVisualStyleBackColor = true;
            this.btnResetFilter.Click += new System.EventHandler(this.btnResetFilter_Click);
            // 
            // btnApplyFilter
            // 
            this.btnApplyFilter.Location = new System.Drawing.Point(380, 37);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(110, 28);
            this.btnApplyFilter.TabIndex = 12;
            this.btnApplyFilter.Text = "Применить";
            this.btnApplyFilter.UseVisualStyleBackColor = true;
            this.btnApplyFilter.Click += new System.EventHandler(this.btnApplyFilter_Click);
            // 
            // chkTimeFilter
            // 
            this.chkTimeFilter.AutoSize = true;
            this.chkTimeFilter.Location = new System.Drawing.Point(8, 72);
            this.chkTimeFilter.Name = "chkTimeFilter";
            this.chkTimeFilter.Size = new System.Drawing.Size(15, 14);
            this.chkTimeFilter.TabIndex = 7;
            this.chkTimeFilter.UseVisualStyleBackColor = true;
            this.chkTimeFilter.CheckedChanged += new System.EventHandler(this.FilterChanged);
            // 
            // dtpTimeTo
            // 
            this.dtpTimeTo.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTimeTo.Location = new System.Drawing.Point(245, 67);
            this.dtpTimeTo.Name = "dtpTimeTo";
            this.dtpTimeTo.ShowUpDown = true;
            this.dtpTimeTo.Size = new System.Drawing.Size(100, 25);
            this.dtpTimeTo.TabIndex = 11;
            this.dtpTimeTo.ValueChanged += new System.EventHandler(this.FilterChanged);
            // 
            // lblTimeTo
            // 
            this.lblTimeTo.AutoSize = true;
            this.lblTimeTo.Location = new System.Drawing.Point(215, 70);
            this.lblTimeTo.Name = "lblTimeTo";
            this.lblTimeTo.Size = new System.Drawing.Size(28, 19);
            this.lblTimeTo.TabIndex = 10;
            this.lblTimeTo.Text = "до:";
            // 
            // dtpTimeFrom
            // 
            this.dtpTimeFrom.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpTimeFrom.Location = new System.Drawing.Point(105, 67);
            this.dtpTimeFrom.Name = "dtpTimeFrom";
            this.dtpTimeFrom.ShowUpDown = true;
            this.dtpTimeFrom.Size = new System.Drawing.Size(100, 25);
            this.dtpTimeFrom.TabIndex = 9;
            this.dtpTimeFrom.ValueChanged += new System.EventHandler(this.FilterChanged);
            // 
            // lblTimeFrom
            // 
            this.lblTimeFrom.AutoSize = true;
            this.lblTimeFrom.Location = new System.Drawing.Point(28, 70);
            this.lblTimeFrom.Name = "lblTimeFrom";
            this.lblTimeFrom.Size = new System.Drawing.Size(70, 19);
            this.lblTimeFrom.TabIndex = 8;
            this.lblTimeFrom.Text = "Время от:";
            // 
            // chkDateFilter
            // 
            this.chkDateFilter.AutoSize = true;
            this.chkDateFilter.Location = new System.Drawing.Point(8, 42);
            this.chkDateFilter.Name = "chkDateFilter";
            this.chkDateFilter.Size = new System.Drawing.Size(15, 14);
            this.chkDateFilter.TabIndex = 2;
            this.chkDateFilter.UseVisualStyleBackColor = true;
            this.chkDateFilter.CheckedChanged += new System.EventHandler(this.FilterChanged);
            // 
            // dtpDateTo
            // 
            this.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateTo.Location = new System.Drawing.Point(245, 37);
            this.dtpDateTo.Name = "dtpDateTo";
            this.dtpDateTo.Size = new System.Drawing.Size(110, 25);
            this.dtpDateTo.TabIndex = 6;
            this.dtpDateTo.ValueChanged += new System.EventHandler(this.FilterChanged);
            // 
            // lblDateTo
            // 
            this.lblDateTo.AutoSize = true;
            this.lblDateTo.Location = new System.Drawing.Point(215, 40);
            this.lblDateTo.Name = "lblDateTo";
            this.lblDateTo.Size = new System.Drawing.Size(28, 19);
            this.lblDateTo.TabIndex = 5;
            this.lblDateTo.Text = "до:";
            // 
            // dtpDateFrom
            // 
            this.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateFrom.Location = new System.Drawing.Point(95, 37);
            this.dtpDateFrom.Name = "dtpDateFrom";
            this.dtpDateFrom.Size = new System.Drawing.Size(110, 25);
            this.dtpDateFrom.TabIndex = 4;
            this.dtpDateFrom.ValueChanged += new System.EventHandler(this.FilterChanged);
            // 
            // lblDateFrom
            // 
            this.lblDateFrom.AutoSize = true;
            this.lblDateFrom.Location = new System.Drawing.Point(28, 40);
            this.lblDateFrom.Name = "lblDateFrom";
            this.lblDateFrom.Size = new System.Drawing.Size(60, 19);
            this.lblDateFrom.TabIndex = 3;
            this.lblDateFrom.Text = "Дата от:";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(65, 7);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(250, 25);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.FilterChanged);
            this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(8, 10);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(51, 19);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "Поиск:";
            // 
            // tabAdmin
            // 
            this.tabAdmin.Controls.Add(this.adminTabControl);
            this.tabAdmin.Location = new System.Drawing.Point(4, 26);
            this.tabAdmin.Name = "tabAdmin";
            this.tabAdmin.Size = new System.Drawing.Size(1092, 635);
            this.tabAdmin.TabIndex = 1;
            this.tabAdmin.Text = "Администрирование";
            this.tabAdmin.UseVisualStyleBackColor = true;
            // 
            // adminTabControl
            // 
            this.adminTabControl.Controls.Add(this.tabAdminUsers);
            this.adminTabControl.Controls.Add(this.tabAdminNoAct);
            this.adminTabControl.Controls.Add(this.tabAdminActs);
            this.adminTabControl.Controls.Add(this.tabAdminStatistics);
            this.adminTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.adminTabControl.Location = new System.Drawing.Point(0, 0);
            this.adminTabControl.Name = "adminTabControl";
            this.adminTabControl.SelectedIndex = 0;
            this.adminTabControl.Size = new System.Drawing.Size(1092, 635);
            this.adminTabControl.TabIndex = 0;
            // 
            // tabAdminUsers
            // 
            this.tabAdminUsers.Controls.Add(this.grpUsers);
            this.tabAdminUsers.Location = new System.Drawing.Point(4, 26);
            this.tabAdminUsers.Name = "tabAdminUsers";
            this.tabAdminUsers.Size = new System.Drawing.Size(1084, 605);
            this.tabAdminUsers.TabIndex = 0;
            this.tabAdminUsers.Text = "Пользователи";
            this.tabAdminUsers.UseVisualStyleBackColor = true;
            // 
            // grpUsers
            // 
            this.grpUsers.Controls.Add(this.dgvUsers);
            this.grpUsers.Controls.Add(this.panelUserActions);
            this.grpUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpUsers.Location = new System.Drawing.Point(0, 0);
            this.grpUsers.Name = "grpUsers";
            this.grpUsers.Padding = new System.Windows.Forms.Padding(5);
            this.grpUsers.Size = new System.Drawing.Size(1084, 605);
            this.grpUsers.TabIndex = 0;
            this.grpUsers.TabStop = false;
            this.grpUsers.Text = "Управление пользователями";
            // 
            // dgvUsers
            // 
            this.dgvUsers.AllowUserToAddRows = false;
            this.dgvUsers.AllowUserToDeleteRows = false;
            this.dgvUsers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvUsers.Location = new System.Drawing.Point(5, 23);
            this.dgvUsers.Name = "dgvUsers";
            this.dgvUsers.ReadOnly = true;
            this.dgvUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvUsers.Size = new System.Drawing.Size(1074, 537);
            this.dgvUsers.TabIndex = 0;
            // 
            // panelUserActions
            // 
            this.panelUserActions.Controls.Add(this.btnAddUser);
            this.panelUserActions.Controls.Add(this.btnEditUser);
            this.panelUserActions.Controls.Add(this.btnDeleteUser);
            this.panelUserActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelUserActions.Location = new System.Drawing.Point(5, 560);
            this.panelUserActions.Name = "panelUserActions";
            this.panelUserActions.Size = new System.Drawing.Size(1074, 40);
            this.panelUserActions.TabIndex = 1;
            // 
            // btnAddUser
            // 
            this.btnAddUser.Location = new System.Drawing.Point(5, 5);
            this.btnAddUser.Name = "btnAddUser";
            this.btnAddUser.Size = new System.Drawing.Size(150, 30);
            this.btnAddUser.TabIndex = 0;
            this.btnAddUser.Text = "Добавить";
            this.btnAddUser.UseVisualStyleBackColor = true;
            this.btnAddUser.Click += new System.EventHandler(this.btnAddUser_Click);
            // 
            // btnEditUser
            // 
            this.btnEditUser.Location = new System.Drawing.Point(165, 5);
            this.btnEditUser.Name = "btnEditUser";
            this.btnEditUser.Size = new System.Drawing.Size(150, 30);
            this.btnEditUser.TabIndex = 1;
            this.btnEditUser.Text = "Изменить";
            this.btnEditUser.UseVisualStyleBackColor = true;
            this.btnEditUser.Click += new System.EventHandler(this.btnEditUser_Click);
            // 
            // btnDeleteUser
            // 
            this.btnDeleteUser.Location = new System.Drawing.Point(325, 5);
            this.btnDeleteUser.Name = "btnDeleteUser";
            this.btnDeleteUser.Size = new System.Drawing.Size(150, 30);
            this.btnDeleteUser.TabIndex = 2;
            this.btnDeleteUser.Text = "Удалить";
            this.btnDeleteUser.UseVisualStyleBackColor = true;
            this.btnDeleteUser.Click += new System.EventHandler(this.btnDeleteUser_Click);
            // 
            // tabAdminNoAct
            // 
            this.tabAdminNoAct.Controls.Add(this.grpNoAct);
            this.tabAdminNoAct.Location = new System.Drawing.Point(4, 26);
            this.tabAdminNoAct.Name = "tabAdminNoAct";
            this.tabAdminNoAct.Size = new System.Drawing.Size(1084, 605);
            this.tabAdminNoAct.TabIndex = 1;
            this.tabAdminNoAct.Text = "Продукты без акта";
            this.tabAdminNoAct.UseVisualStyleBackColor = true;
            // 
            // grpNoAct
            // 
            this.grpNoAct.Controls.Add(this.dgvNoActProducts);
            this.grpNoAct.Controls.Add(this.panelNoActTop);
            this.grpNoAct.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpNoAct.Location = new System.Drawing.Point(0, 0);
            this.grpNoAct.Name = "grpNoAct";
            this.grpNoAct.Padding = new System.Windows.Forms.Padding(5);
            this.grpNoAct.Size = new System.Drawing.Size(1084, 605);
            this.grpNoAct.TabIndex = 0;
            this.grpNoAct.TabStop = false;
            this.grpNoAct.Text = "Продукты без акта";
            // 
            // dgvNoActProducts
            // 
            this.dgvNoActProducts.AllowUserToAddRows = false;
            this.dgvNoActProducts.AllowUserToDeleteRows = false;
            this.dgvNoActProducts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvNoActProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvNoActProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvNoActProducts.Location = new System.Drawing.Point(5, 105);
            this.dgvNoActProducts.Name = "dgvNoActProducts";
            this.dgvNoActProducts.ReadOnly = true;
            this.dgvNoActProducts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvNoActProducts.Size = new System.Drawing.Size(1074, 495);
            this.dgvNoActProducts.TabIndex = 0;
            // 
            // panelNoActTop
            // 
            this.panelNoActTop.Controls.Add(this.lblAdminNewCategory);
            this.panelNoActTop.Controls.Add(this.txtAdminNewCategory);
            this.panelNoActTop.Controls.Add(this.lblAdminCountry);
            this.panelNoActTop.Controls.Add(this.cmbAdminCountry);
            this.panelNoActTop.Controls.Add(this.btnAdminAddCategory);
            this.panelNoActTop.Controls.Add(this.lblAdminSerial);
            this.panelNoActTop.Controls.Add(this.txtAdminSerial);
            this.panelNoActTop.Controls.Add(this.cmbAdminProductType);
            this.panelNoActTop.Controls.Add(this.btnAdminAddProduct);
            this.panelNoActTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelNoActTop.Location = new System.Drawing.Point(5, 23);
            this.panelNoActTop.Name = "panelNoActTop";
            this.panelNoActTop.Size = new System.Drawing.Size(1074, 82);
            this.panelNoActTop.TabIndex = 1;
            // 
            // lblAdminNewCategory
            // 
            this.lblAdminNewCategory.AutoSize = true;
            this.lblAdminNewCategory.Location = new System.Drawing.Point(8, 12);
            this.lblAdminNewCategory.Name = "lblAdminNewCategory";
            this.lblAdminNewCategory.Size = new System.Drawing.Size(118, 19);
            this.lblAdminNewCategory.TabIndex = 0;
            this.lblAdminNewCategory.Text = "Новая категория:";
            // 
            // txtAdminNewCategory
            // 
            this.txtAdminNewCategory.Location = new System.Drawing.Point(130, 9);
            this.txtAdminNewCategory.Name = "txtAdminNewCategory";
            this.txtAdminNewCategory.Size = new System.Drawing.Size(180, 25);
            this.txtAdminNewCategory.TabIndex = 1;
            // 
            // lblAdminCountry
            // 
            this.lblAdminCountry.AutoSize = true;
            this.lblAdminCountry.Location = new System.Drawing.Point(320, 12);
            this.lblAdminCountry.Name = "lblAdminCountry";
            this.lblAdminCountry.Size = new System.Drawing.Size(57, 19);
            this.lblAdminCountry.TabIndex = 2;
            this.lblAdminCountry.Text = "Страна:";
            // 
            // cmbAdminCountry
            // 
            this.cmbAdminCountry.FormattingEnabled = true;
            this.cmbAdminCountry.Location = new System.Drawing.Point(384, 9);
            this.cmbAdminCountry.Name = "cmbAdminCountry";
            this.cmbAdminCountry.Size = new System.Drawing.Size(180, 25);
            this.cmbAdminCountry.TabIndex = 3;
            // 
            // btnAdminAddCategory
            // 
            this.btnAdminAddCategory.Location = new System.Drawing.Point(580, 7);
            this.btnAdminAddCategory.Name = "btnAdminAddCategory";
            this.btnAdminAddCategory.Size = new System.Drawing.Size(160, 28);
            this.btnAdminAddCategory.TabIndex = 4;
            this.btnAdminAddCategory.Text = "Добавить категорию";
            this.btnAdminAddCategory.UseVisualStyleBackColor = true;
            this.btnAdminAddCategory.Click += new System.EventHandler(this.btnAdminAddCategory_Click);
            // 
            // lblAdminSerial
            // 
            this.lblAdminSerial.AutoSize = true;
            this.lblAdminSerial.Location = new System.Drawing.Point(8, 48);
            this.lblAdminSerial.Name = "lblAdminSerial";
            this.lblAdminSerial.Size = new System.Drawing.Size(123, 19);
            this.lblAdminSerial.TabIndex = 5;
            this.lblAdminSerial.Text = "Серийный номер:";
            // 
            // txtAdminSerial
            // 
            this.txtAdminSerial.Location = new System.Drawing.Point(130, 45);
            this.txtAdminSerial.Name = "txtAdminSerial";
            this.txtAdminSerial.Size = new System.Drawing.Size(180, 25);
            this.txtAdminSerial.TabIndex = 6;
            this.txtAdminSerial.Leave += new System.EventHandler(this.txtAdminSerial_Leave);
            // 
            // cmbAdminProductType
            // 
            this.cmbAdminProductType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAdminProductType.FormattingEnabled = true;
            this.cmbAdminProductType.Location = new System.Drawing.Point(384, 45);
            this.cmbAdminProductType.Name = "cmbAdminProductType";
            this.cmbAdminProductType.Size = new System.Drawing.Size(356, 25);
            this.cmbAdminProductType.TabIndex = 7;
            // 
            // btnAdminAddProduct
            // 
            this.btnAdminAddProduct.Location = new System.Drawing.Point(756, 43);
            this.btnAdminAddProduct.Name = "btnAdminAddProduct";
            this.btnAdminAddProduct.Size = new System.Drawing.Size(140, 28);
            this.btnAdminAddProduct.TabIndex = 8;
            this.btnAdminAddProduct.Text = "Добавить продукт";
            this.btnAdminAddProduct.UseVisualStyleBackColor = true;
            this.btnAdminAddProduct.Click += new System.EventHandler(this.btnAdminAddProduct_Click);
            // 
            // tabAdminActs
            // 
            this.tabAdminActs.Controls.Add(this.btnAdminAssign);
            this.tabAdminActs.Controls.Add(this.btnAdminGenerateQr);
            this.tabAdminActs.Controls.Add(this.dgvAdminUnassigned);
            this.tabAdminActs.Controls.Add(this.lblAdminUnassigned);
            this.tabAdminActs.Controls.Add(this.lblActExplain);
            this.tabAdminActs.Controls.Add(this.btnBrowsePath);
            this.tabAdminActs.Controls.Add(this.txtActPath);
            this.tabAdminActs.Controls.Add(this.lblActPath);
            this.tabAdminActs.Controls.Add(this.lblAdminSelectAct);
            this.tabAdminActs.Controls.Add(this.cmbAdminActs);
            this.tabAdminActs.Controls.Add(this.grpAdminAct);
            this.tabAdminActs.Location = new System.Drawing.Point(4, 26);
            this.tabAdminActs.Name = "tabAdminActs";
            this.tabAdminActs.Padding = new System.Windows.Forms.Padding(10);
            this.tabAdminActs.Size = new System.Drawing.Size(1084, 605);
            this.tabAdminActs.TabIndex = 2;
            this.tabAdminActs.Text = "Акты и папки";
            this.tabAdminActs.UseVisualStyleBackColor = true;
            // 
            // btnAdminAssign
            // 
            this.btnAdminAssign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAdminAssign.Location = new System.Drawing.Point(13, 567);
            this.btnAdminAssign.Name = "btnAdminAssign";
            this.btnAdminAssign.Size = new System.Drawing.Size(300, 35);
            this.btnAdminAssign.TabIndex = 5;
            this.btnAdminAssign.Text = "Привязать выбранные к акту и создать папки";
            this.btnAdminAssign.UseVisualStyleBackColor = true;
            this.btnAdminAssign.Click += new System.EventHandler(this.btnAdminAssign_Click);
            // 
            // btnAdminGenerateQr
            // 
            this.btnAdminGenerateQr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAdminGenerateQr.Location = new System.Drawing.Point(330, 567);
            this.btnAdminGenerateQr.Name = "btnAdminGenerateQr";
            this.btnAdminGenerateQr.Size = new System.Drawing.Size(300, 35);
            this.btnAdminGenerateQr.TabIndex = 6;
            this.btnAdminGenerateQr.Text = "Генерировать QR-коды для акта";
            this.btnAdminGenerateQr.UseVisualStyleBackColor = true;
            this.btnAdminGenerateQr.Click += new System.EventHandler(this.btnAdminGenerateQr_Click);
            // 
            // dgvAdminUnassigned
            // 
            this.dgvAdminUnassigned.AllowUserToAddRows = false;
            this.dgvAdminUnassigned.AllowUserToDeleteRows = false;
            this.dgvAdminUnassigned.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvAdminUnassigned.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvAdminUnassigned.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvAdminUnassigned.Location = new System.Drawing.Point(13, 167);
            this.dgvAdminUnassigned.Name = "dgvAdminUnassigned";
            this.dgvAdminUnassigned.ReadOnly = true;
            this.dgvAdminUnassigned.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAdminUnassigned.Size = new System.Drawing.Size(1058, 377);
            this.dgvAdminUnassigned.TabIndex = 4;
            // 
            // lblAdminUnassigned
            // 
            this.lblAdminUnassigned.AutoSize = true;
            this.lblAdminUnassigned.Location = new System.Drawing.Point(13, 145);
            this.lblAdminUnassigned.Name = "lblAdminUnassigned";
            this.lblAdminUnassigned.Size = new System.Drawing.Size(784, 19);
            this.lblAdminUnassigned.TabIndex = 3;
            this.lblAdminUnassigned.Text = "Продукты без акта: выберите конкретный или несколько продуктов (клик по строке), " +
    "затем нажмите «Привязать к акту».";
            // 
            // lblActExplain
            // 
            this.lblActExplain.AutoSize = true;
            this.lblActExplain.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lblActExplain.ForeColor = System.Drawing.Color.Gray;
            this.lblActExplain.Location = new System.Drawing.Point(410, 112);
            this.lblActExplain.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblActExplain.Name = "lblActExplain";
            this.lblActExplain.Size = new System.Drawing.Size(460, 26);
            this.lblActExplain.TabIndex = 7;
            this.lblActExplain.Text = "Запись в таблицу Act появляется только при привязке продукта к акту (в БД ActID —" +
    " внешний ключ на Product).";
            // 
            // btnBrowsePath
            // 
            this.btnBrowsePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowsePath.Location = new System.Drawing.Point(963, 81);
            this.btnBrowsePath.Name = "btnBrowsePath";
            this.btnBrowsePath.Size = new System.Drawing.Size(40, 27);
            this.btnBrowsePath.TabIndex = 8;
            this.btnBrowsePath.Text = "...";
            this.btnBrowsePath.UseVisualStyleBackColor = true;
            this.btnBrowsePath.Click += new System.EventHandler(this.btnBrowsePath_Click);
            // 
            // txtActPath
            // 
            this.txtActPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtActPath.Location = new System.Drawing.Point(458, 82);
            this.txtActPath.Name = "txtActPath";
            this.txtActPath.Size = new System.Drawing.Size(500, 25);
            this.txtActPath.TabIndex = 7;
            // 
            // lblActPath
            // 
            this.lblActPath.AutoSize = true;
            this.lblActPath.Location = new System.Drawing.Point(410, 85);
            this.lblActPath.Name = "lblActPath";
            this.lblActPath.Size = new System.Drawing.Size(42, 19);
            this.lblActPath.TabIndex = 6;
            this.lblActPath.Text = "Путь:";
            // 
            // lblAdminSelectAct
            // 
            this.lblAdminSelectAct.AutoSize = true;
            this.lblAdminSelectAct.Location = new System.Drawing.Point(13, 85);
            this.lblAdminSelectAct.Name = "lblAdminSelectAct";
            this.lblAdminSelectAct.Size = new System.Drawing.Size(98, 19);
            this.lblAdminSelectAct.TabIndex = 1;
            this.lblAdminSelectAct.Text = "Выберите акт:";
            // 
            // cmbAdminActs
            // 
            this.cmbAdminActs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAdminActs.Location = new System.Drawing.Point(140, 82);
            this.cmbAdminActs.Name = "cmbAdminActs";
            this.cmbAdminActs.Size = new System.Drawing.Size(250, 25);
            this.cmbAdminActs.TabIndex = 2;
            this.cmbAdminActs.SelectedIndexChanged += new System.EventHandler(this.cmbAdminActs_SelectedIndexChanged);
            // 
            // grpAdminAct
            // 
            this.grpAdminAct.Controls.Add(this.lblAdminActNumber);
            this.grpAdminAct.Controls.Add(this.txtAdminActNumber);
            this.grpAdminAct.Controls.Add(this.btnAdminCreateAct);
            this.grpAdminAct.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpAdminAct.Location = new System.Drawing.Point(10, 10);
            this.grpAdminAct.Name = "grpAdminAct";
            this.grpAdminAct.Padding = new System.Windows.Forms.Padding(10);
            this.grpAdminAct.Size = new System.Drawing.Size(1064, 65);
            this.grpAdminAct.TabIndex = 0;
            this.grpAdminAct.TabStop = false;
            this.grpAdminAct.Text = "Создать акт";
            // 
            // lblAdminActNumber
            // 
            this.lblAdminActNumber.AutoSize = true;
            this.lblAdminActNumber.Location = new System.Drawing.Point(13, 30);
            this.lblAdminActNumber.Name = "lblAdminActNumber";
            this.lblAdminActNumber.Size = new System.Drawing.Size(86, 19);
            this.lblAdminActNumber.TabIndex = 0;
            this.lblAdminActNumber.Text = "Номер акта:";
            // 
            // txtAdminActNumber
            // 
            this.txtAdminActNumber.Location = new System.Drawing.Point(110, 27);
            this.txtAdminActNumber.MaxLength = 6;
            this.txtAdminActNumber.Name = "txtAdminActNumber";
            this.txtAdminActNumber.Size = new System.Drawing.Size(200, 25);
            this.txtAdminActNumber.TabIndex = 1;
            // 
            // btnAdminCreateAct
            // 
            this.btnAdminCreateAct.Location = new System.Drawing.Point(330, 25);
            this.btnAdminCreateAct.Name = "btnAdminCreateAct";
            this.btnAdminCreateAct.Size = new System.Drawing.Size(120, 28);
            this.btnAdminCreateAct.TabIndex = 5;
            this.btnAdminCreateAct.Text = "Создать акт";
            this.btnAdminCreateAct.UseVisualStyleBackColor = true;
            this.btnAdminCreateAct.Click += new System.EventHandler(this.btnAdminCreateAct_Click);
            // 
            // tabAdminStatistics
            // 
            this.tabAdminStatistics.Controls.Add(this.btnRefreshStats);
            this.tabAdminStatistics.Controls.Add(this.grpStatsByDefect);
            this.tabAdminStatistics.Controls.Add(this.grpStatsByStage);
            this.tabAdminStatistics.Location = new System.Drawing.Point(4, 26);
            this.tabAdminStatistics.Name = "tabAdminStatistics";
            this.tabAdminStatistics.Padding = new System.Windows.Forms.Padding(5);
            this.tabAdminStatistics.Size = new System.Drawing.Size(1084, 605);
            this.tabAdminStatistics.TabIndex = 3;
            this.tabAdminStatistics.Text = "Статистика по браку";
            this.tabAdminStatistics.UseVisualStyleBackColor = true;
            // 
            // btnRefreshStats
            // 
            this.btnRefreshStats.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefreshStats.Location = new System.Drawing.Point(950, 566);
            this.btnRefreshStats.Name = "btnRefreshStats";
            this.btnRefreshStats.Size = new System.Drawing.Size(126, 28);
            this.btnRefreshStats.TabIndex = 2;
            this.btnRefreshStats.Text = "Обновить";
            this.btnRefreshStats.UseVisualStyleBackColor = true;
            this.btnRefreshStats.Click += new System.EventHandler(this.btnRefreshStats_Click);
            // 
            // grpStatsByDefect
            // 
            this.grpStatsByDefect.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpStatsByDefect.Controls.Add(this.dgvStatsByDefect);
            this.grpStatsByDefect.Location = new System.Drawing.Point(8, 234);
            this.grpStatsByDefect.Name = "grpStatsByDefect";
            this.grpStatsByDefect.Size = new System.Drawing.Size(1068, 324);
            this.grpStatsByDefect.TabIndex = 1;
            this.grpStatsByDefect.TabStop = false;
            this.grpStatsByDefect.Text = "По типам брака (какой брак, количество, процент от всего)";
            // 
            // dgvStatsByDefect
            // 
            this.dgvStatsByDefect.AllowUserToAddRows = false;
            this.dgvStatsByDefect.AllowUserToDeleteRows = false;
            this.dgvStatsByDefect.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvStatsByDefect.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStatsByDefect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStatsByDefect.Location = new System.Drawing.Point(3, 21);
            this.dgvStatsByDefect.Name = "dgvStatsByDefect";
            this.dgvStatsByDefect.ReadOnly = true;
            this.dgvStatsByDefect.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStatsByDefect.Size = new System.Drawing.Size(1062, 300);
            this.dgvStatsByDefect.TabIndex = 0;
            // 
            // grpStatsByStage
            // 
            this.grpStatsByStage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpStatsByStage.Controls.Add(this.dgvStatsByStage);
            this.grpStatsByStage.Location = new System.Drawing.Point(8, 8);
            this.grpStatsByStage.Name = "grpStatsByStage";
            this.grpStatsByStage.Size = new System.Drawing.Size(1068, 220);
            this.grpStatsByStage.TabIndex = 0;
            this.grpStatsByStage.TabStop = false;
            this.grpStatsByStage.Text = "По этапам обнаружения брака (сколько и какой процент от всего)";
            // 
            // dgvStatsByStage
            // 
            this.dgvStatsByStage.AllowUserToAddRows = false;
            this.dgvStatsByStage.AllowUserToDeleteRows = false;
            this.dgvStatsByStage.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvStatsByStage.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStatsByStage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStatsByStage.Location = new System.Drawing.Point(3, 21);
            this.dgvStatsByStage.Name = "dgvStatsByStage";
            this.dgvStatsByStage.ReadOnly = true;
            this.dgvStatsByStage.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvStatsByStage.Size = new System.Drawing.Size(1062, 196);
            this.dgvStatsByStage.TabIndex = 0;
            // 
            // splitAdmin
            // 
            this.splitAdmin.Location = new System.Drawing.Point(0, 0);
            this.splitAdmin.Name = "splitAdmin";
            this.splitAdmin.Size = new System.Drawing.Size(150, 100);
            this.splitAdmin.TabIndex = 0;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnLogout);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1100, 35);
            this.panelTop.TabIndex = 10;
            // 
            // btnLogout
            // 
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnLogout.Location = new System.Drawing.Point(995, 4);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(100, 28);
            this.btnLogout.TabIndex = 0;
            this.btnLogout.Text = "Выйти";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // EmployeeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 700);
            this.Controls.Add(this.mainTabControl);
            this.Controls.Add(this.panelTop);
            this.Name = "EmployeeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Сотрудник";
            this.mainTabControl.ResumeLayout(false);
            this.tabEmployee.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvProducts)).EndInit();
            this.panelPath.ResumeLayout(false);
            this.panelPath.PerformLayout();
            this.panelActions.ResumeLayout(false);
            this.tabControlActions.ResumeLayout(false);
            this.tabPageActionsGeneral.ResumeLayout(false);
            this.tabPageActionsAdmin.ResumeLayout(false);
            this.tabPageActionsTester.ResumeLayout(false);
            this.tabControlWork.ResumeLayout(false);
            this.panelFilters.ResumeLayout(false);
            this.panelFilters.PerformLayout();
            this.tabAdmin.ResumeLayout(false);
            this.adminTabControl.ResumeLayout(false);
            this.tabAdminUsers.ResumeLayout(false);
            this.grpUsers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvUsers)).EndInit();
            this.panelUserActions.ResumeLayout(false);
            this.tabAdminNoAct.ResumeLayout(false);
            this.grpNoAct.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvNoActProducts)).EndInit();
            this.panelNoActTop.ResumeLayout(false);
            this.panelNoActTop.PerformLayout();
            this.tabAdminActs.ResumeLayout(false);
            this.tabAdminActs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAdminUnassigned)).EndInit();
            this.grpAdminAct.ResumeLayout(false);
            this.grpAdminAct.PerformLayout();
            this.tabAdminStatistics.ResumeLayout(false);
            this.grpStatsByDefect.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatsByDefect)).EndInit();
            this.grpStatsByStage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStatsByStage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitAdmin)).EndInit();
            this.splitAdmin.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.TabPage tabEmployee;
        private System.Windows.Forms.TabPage tabAdmin;

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Label lblActs;
        private System.Windows.Forms.ListBox lstActs;

        private System.Windows.Forms.Panel panelFilters;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.DateTimePicker dtpDateFrom;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.DateTimePicker dtpDateTo;
        private System.Windows.Forms.CheckBox chkDateFilter;
        private System.Windows.Forms.Label lblTimeFrom;
        private System.Windows.Forms.DateTimePicker dtpTimeFrom;
        private System.Windows.Forms.Label lblTimeTo;
        private System.Windows.Forms.DateTimePicker dtpTimeTo;
        private System.Windows.Forms.CheckBox chkTimeFilter;
        private System.Windows.Forms.Button btnApplyFilter;
        private System.Windows.Forms.Button btnResetFilter;

        private System.Windows.Forms.DataGridView dgvProducts;
        private System.Windows.Forms.TabControl tabControlWork;
        private System.Windows.Forms.TabPage tabPageAssembly;
        private System.Windows.Forms.TabPage tabPageTesting;
        private System.Windows.Forms.TabPage tabPageInspection;

        private System.Windows.Forms.Panel panelActions;
        private System.Windows.Forms.TabControl tabControlActions;
        private System.Windows.Forms.TabPage tabPageActionsGeneral;
        private System.Windows.Forms.TabPage tabPageActionsAdmin;
        private System.Windows.Forms.TabPage tabPageActionsTester;
        private System.Windows.Forms.Button btnSaveChanges;
        private System.Windows.Forms.Button btnChangeStatus;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnCreateActFolders;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnGenerateQrEmployee;
        private System.Windows.Forms.Button btnBridgeTesting;
        private System.Windows.Forms.Button btnCrossPlateTesting;
        private System.Windows.Forms.Button btnAdvancedTesting;
        private System.Windows.Forms.Button btnNonConformity;

        private System.Windows.Forms.TabControl adminTabControl;
        private System.Windows.Forms.TabPage tabAdminUsers;
        private System.Windows.Forms.TabPage tabAdminNoAct;
        private System.Windows.Forms.TabPage tabAdminActs;
        private System.Windows.Forms.TabPage tabAdminStatistics;
        private System.Windows.Forms.GroupBox grpStatsByStage;
        private System.Windows.Forms.DataGridView dgvStatsByStage;
        private System.Windows.Forms.GroupBox grpStatsByDefect;
        private System.Windows.Forms.DataGridView dgvStatsByDefect;
        private System.Windows.Forms.Button btnRefreshStats;

        private System.Windows.Forms.SplitContainer splitAdmin;
        private System.Windows.Forms.GroupBox grpUsers;
        private System.Windows.Forms.DataGridView dgvUsers;
        private System.Windows.Forms.Panel panelUserActions;
        private System.Windows.Forms.Button btnAddUser;
        private System.Windows.Forms.Button btnEditUser;
        private System.Windows.Forms.Button btnDeleteUser;
        private System.Windows.Forms.GroupBox grpNoAct;
        private System.Windows.Forms.Panel panelNoActTop;
        private System.Windows.Forms.Label lblAdminNewCategory;
        private System.Windows.Forms.TextBox txtAdminNewCategory;
        private System.Windows.Forms.Label lblAdminCountry;
        private System.Windows.Forms.ComboBox cmbAdminCountry;
        private System.Windows.Forms.Button btnAdminAddCategory;
        private System.Windows.Forms.Label lblAdminSerial;
        private System.Windows.Forms.TextBox txtAdminSerial;
        private System.Windows.Forms.ComboBox cmbAdminProductType;
        private System.Windows.Forms.Button btnAdminAddProduct;
        private System.Windows.Forms.DataGridView dgvNoActProducts;

        private System.Windows.Forms.GroupBox grpAdminAct;
        private System.Windows.Forms.Label lblAdminActNumber;
        private System.Windows.Forms.TextBox txtAdminActNumber;
        private System.Windows.Forms.Label lblActPath;
        private System.Windows.Forms.TextBox txtActPath;
        private System.Windows.Forms.Button btnBrowsePath;
        private System.Windows.Forms.Button btnAdminCreateAct;
        private System.Windows.Forms.Label lblAdminUnassigned;
        private System.Windows.Forms.Label lblActExplain;
        private System.Windows.Forms.DataGridView dgvAdminUnassigned;
        private System.Windows.Forms.ComboBox cmbAdminActs;
        private System.Windows.Forms.Label lblAdminSelectAct;
        private System.Windows.Forms.Button btnAdminAssign;
        private System.Windows.Forms.Button btnAdminGenerateQr;

        private System.Windows.Forms.Panel panelPath;
        private System.Windows.Forms.Label lblUserPath;
        private System.Windows.Forms.TextBox txtUserPath;
        private System.Windows.Forms.Button btnBrowseUserPath;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnLogout;
    }
}
