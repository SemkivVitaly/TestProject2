namespace SaveData1
{
    partial class WarehouseForm
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
            this.tabProducts = new System.Windows.Forms.TabPage();
            this.dgvProducts = new System.Windows.Forms.DataGridView();
            this.panelSearch = new System.Windows.Forms.Panel();
            this.btnResetFilter = new System.Windows.Forms.Button();
            this.btnApplyFilter = new System.Windows.Forms.Button();
            this.cmbSort = new System.Windows.Forms.ComboBox();
            this.lblSort = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.grpProduct = new System.Windows.Forms.GroupBox();
            this.lblSerial = new System.Windows.Forms.Label();
            this.txtSerial = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbProductCategory = new System.Windows.Forms.ComboBox();
            this.btnAddProduct = new System.Windows.Forms.Button();
            this.btnGenerateQr = new System.Windows.Forms.Button();
            this.btnNonConformity = new System.Windows.Forms.Button();
            this.grpCategory = new System.Windows.Forms.GroupBox();
            this.lblNewCategory = new System.Windows.Forms.Label();
            this.txtNewCategory = new System.Windows.Forms.TextBox();
            this.lblCountry = new System.Windows.Forms.Label();
            this.cmbCountry = new System.Windows.Forms.ComboBox();
            this.btnAddCategory = new System.Windows.Forms.Button();
            this.tabActs = new System.Windows.Forms.TabPage();
            this.btnNonConformityInAct = new System.Windows.Forms.Button();
            this.btnAssignToAct = new System.Windows.Forms.Button();
            this.btnGenerateActQr = new System.Windows.Forms.Button();
            this.dgvUnassignedProducts = new System.Windows.Forms.DataGridView();
            this.lblSelectProducts = new System.Windows.Forms.Label();
            this.dgvProductsInAct = new System.Windows.Forms.DataGridView();
            this.lblProductsInAct = new System.Windows.Forms.Label();
            this.lblSelectAct = new System.Windows.Forms.Label();
            this.cmbActs = new System.Windows.Forms.ComboBox();
            this.grpAct = new System.Windows.Forms.GroupBox();
            this.lblActNumber = new System.Windows.Forms.Label();
            this.txtActNumber = new System.Windows.Forms.TextBox();
            this.btnCreateAct = new System.Windows.Forms.Button();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabProducts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProducts)).BeginInit();
            this.panelSearch.SuspendLayout();
            this.grpProduct.SuspendLayout();
            this.grpCategory.SuspendLayout();
            this.tabActs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUnassignedProducts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProductsInAct)).BeginInit();
            this.grpAct.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabProducts);
            this.tabControl.Controls.Add(this.tabActs);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.tabControl.Location = new System.Drawing.Point(0, 35);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(900, 565);
            this.tabControl.TabIndex = 0;
            // 
            // tabProducts
            // 
            this.tabProducts.Controls.Add(this.dgvProducts);
            this.tabProducts.Controls.Add(this.panelSearch);
            this.tabProducts.Controls.Add(this.grpProduct);
            this.tabProducts.Controls.Add(this.grpCategory);
            this.tabProducts.Location = new System.Drawing.Point(4, 26);
            this.tabProducts.Name = "tabProducts";
            this.tabProducts.Padding = new System.Windows.Forms.Padding(10);
            this.tabProducts.Size = new System.Drawing.Size(892, 535);
            this.tabProducts.TabIndex = 0;
            this.tabProducts.Text = "Продукты";
            this.tabProducts.UseVisualStyleBackColor = true;
            // 
            // dgvProducts
            // 
            this.dgvProducts.AllowUserToAddRows = false;
            this.dgvProducts.AllowUserToDeleteRows = false;
            this.dgvProducts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvProducts.Location = new System.Drawing.Point(10, 235);
            this.dgvProducts.Name = "dgvProducts";
            this.dgvProducts.ReadOnly = true;
            this.dgvProducts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvProducts.Size = new System.Drawing.Size(872, 290);
            this.dgvProducts.TabIndex = 2;
            // 
            // panelSearch
            // 
            this.panelSearch.Controls.Add(this.btnResetFilter);
            this.panelSearch.Controls.Add(this.btnApplyFilter);
            this.panelSearch.Controls.Add(this.cmbSort);
            this.panelSearch.Controls.Add(this.lblSort);
            this.panelSearch.Controls.Add(this.txtSearch);
            this.panelSearch.Controls.Add(this.lblSearch);
            this.panelSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSearch.Location = new System.Drawing.Point(10, 180);
            this.panelSearch.Name = "panelSearch";
            this.panelSearch.Padding = new System.Windows.Forms.Padding(5);
            this.panelSearch.Size = new System.Drawing.Size(872, 55);
            this.panelSearch.TabIndex = 3;
            // 
            // btnResetFilter
            // 
            this.btnResetFilter.Location = new System.Drawing.Point(762, 11);
            this.btnResetFilter.Name = "btnResetFilter";
            this.btnResetFilter.Size = new System.Drawing.Size(100, 28);
            this.btnResetFilter.TabIndex = 5;
            this.btnResetFilter.Text = "Сбросить";
            this.btnResetFilter.UseVisualStyleBackColor = true;
            this.btnResetFilter.Click += new System.EventHandler(this.btnResetFilter_Click);
            // 
            // btnApplyFilter
            // 
            this.btnApplyFilter.Location = new System.Drawing.Point(655, 11);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(100, 28);
            this.btnApplyFilter.TabIndex = 4;
            this.btnApplyFilter.Text = "Применить";
            this.btnApplyFilter.UseVisualStyleBackColor = true;
            this.btnApplyFilter.Click += new System.EventHandler(this.btnApplyFilter_Click);
            // 
            // cmbSort
            // 
            this.cmbSort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSort.FormattingEnabled = true;
            this.cmbSort.Location = new System.Drawing.Point(422, 12);
            this.cmbSort.Name = "cmbSort";
            this.cmbSort.Size = new System.Drawing.Size(220, 25);
            this.cmbSort.TabIndex = 3;
            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Location = new System.Drawing.Point(330, 15);
            this.lblSort.Name = "lblSort";
            this.lblSort.Size = new System.Drawing.Size(88, 19);
            this.lblSort.TabIndex = 2;
            this.lblSort.Text = "Сортировка:";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(65, 12);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(250, 25);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(8, 15);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(51, 19);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "Поиск:";
            // 
            // grpProduct
            // 
            this.grpProduct.Controls.Add(this.lblSerial);
            this.grpProduct.Controls.Add(this.txtSerial);
            this.grpProduct.Controls.Add(this.lblCategory);
            this.grpProduct.Controls.Add(this.cmbProductCategory);
            this.grpProduct.Controls.Add(this.btnAddProduct);
            this.grpProduct.Controls.Add(this.btnGenerateQr);
            this.grpProduct.Controls.Add(this.btnNonConformity);
            this.grpProduct.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpProduct.Location = new System.Drawing.Point(10, 75);
            this.grpProduct.Name = "grpProduct";
            this.grpProduct.Padding = new System.Windows.Forms.Padding(10);
            this.grpProduct.Size = new System.Drawing.Size(872, 105);
            this.grpProduct.TabIndex = 1;
            this.grpProduct.TabStop = false;
            this.grpProduct.Text = "Добавить продукт";
            // 
            // lblSerial
            // 
            this.lblSerial.AutoSize = true;
            this.lblSerial.Location = new System.Drawing.Point(13, 32);
            this.lblSerial.Name = "lblSerial";
            this.lblSerial.Size = new System.Drawing.Size(123, 19);
            this.lblSerial.TabIndex = 0;
            this.lblSerial.Text = "Серийный номер:";
            // 
            // txtSerial
            // 
            this.txtSerial.Location = new System.Drawing.Point(130, 29);
            this.txtSerial.Name = "txtSerial";
            this.txtSerial.Size = new System.Drawing.Size(200, 25);
            this.txtSerial.TabIndex = 1;
            this.txtSerial.Leave += new System.EventHandler(this.txtSerial_Leave);
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Location = new System.Drawing.Point(345, 32);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(76, 19);
            this.lblCategory.TabIndex = 2;
            this.lblCategory.Text = "Категория:";
            // 
            // cmbProductCategory
            // 
            this.cmbProductCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProductCategory.Location = new System.Drawing.Point(430, 29);
            this.cmbProductCategory.Name = "cmbProductCategory";
            this.cmbProductCategory.Size = new System.Drawing.Size(220, 25);
            this.cmbProductCategory.TabIndex = 3;
            // 
            // btnAddProduct
            // 
            this.btnAddProduct.Location = new System.Drawing.Point(665, 27);
            this.btnAddProduct.Name = "btnAddProduct";
            this.btnAddProduct.Size = new System.Drawing.Size(180, 28);
            this.btnAddProduct.TabIndex = 4;
            this.btnAddProduct.Text = "Добавить продукт";
            this.btnAddProduct.UseVisualStyleBackColor = true;
            this.btnAddProduct.Click += new System.EventHandler(this.btnAddProduct_Click);
            // 
            // btnGenerateQr
            // 
            this.btnGenerateQr.Location = new System.Drawing.Point(13, 62);
            this.btnGenerateQr.Name = "btnGenerateQr";
            this.btnGenerateQr.Size = new System.Drawing.Size(300, 30);
            this.btnGenerateQr.TabIndex = 5;
            this.btnGenerateQr.Text = "Генерировать QR-код (выбранный)";
            this.btnGenerateQr.UseVisualStyleBackColor = true;
            this.btnGenerateQr.Click += new System.EventHandler(this.btnGenerateQr_Click);
            // 
            // btnNonConformity
            // 
            this.btnNonConformity.Location = new System.Drawing.Point(320, 62);
            this.btnNonConformity.Name = "btnNonConformity";
            this.btnNonConformity.Size = new System.Drawing.Size(300, 30);
            this.btnNonConformity.TabIndex = 6;
            this.btnNonConformity.Text = "Ярлык несоответствия (выбранный)";
            this.btnNonConformity.UseVisualStyleBackColor = true;
            this.btnNonConformity.Click += new System.EventHandler(this.btnNonConformity_Click);
            // 
            // grpCategory
            // 
            this.grpCategory.Controls.Add(this.lblNewCategory);
            this.grpCategory.Controls.Add(this.txtNewCategory);
            this.grpCategory.Controls.Add(this.lblCountry);
            this.grpCategory.Controls.Add(this.cmbCountry);
            this.grpCategory.Controls.Add(this.btnAddCategory);
            this.grpCategory.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpCategory.Location = new System.Drawing.Point(10, 10);
            this.grpCategory.Name = "grpCategory";
            this.grpCategory.Padding = new System.Windows.Forms.Padding(10);
            this.grpCategory.Size = new System.Drawing.Size(872, 65);
            this.grpCategory.TabIndex = 0;
            this.grpCategory.TabStop = false;
            this.grpCategory.Text = "Категории продуктов";
            // 
            // lblNewCategory
            // 
            this.lblNewCategory.AutoSize = true;
            this.lblNewCategory.Location = new System.Drawing.Point(13, 32);
            this.lblNewCategory.Name = "lblNewCategory";
            this.lblNewCategory.Size = new System.Drawing.Size(140, 19);
            this.lblNewCategory.TabIndex = 0;
            this.lblNewCategory.Text = "Название категории:";
            // 
            // txtNewCategory
            // 
            this.txtNewCategory.Location = new System.Drawing.Point(149, 29);
            this.txtNewCategory.Name = "txtNewCategory";
            this.txtNewCategory.Size = new System.Drawing.Size(220, 25);
            this.txtNewCategory.TabIndex = 1;
            // 
            // lblCountry
            // 
            this.lblCountry.AutoSize = true;
            this.lblCountry.Location = new System.Drawing.Point(375, 32);
            this.lblCountry.Name = "lblCountry";
            this.lblCountry.Size = new System.Drawing.Size(157, 19);
            this.lblCountry.TabIndex = 2;
            this.lblCountry.Text = "Страна производитель:";
            // 
            // cmbCountry
            // 
            this.cmbCountry.FormattingEnabled = true;
            this.cmbCountry.Location = new System.Drawing.Point(533, 29);
            this.cmbCountry.Name = "cmbCountry";
            this.cmbCountry.Size = new System.Drawing.Size(220, 25);
            this.cmbCountry.TabIndex = 3;
            // 
            // btnAddCategory
            // 
            this.btnAddCategory.Location = new System.Drawing.Point(759, 26);
            this.btnAddCategory.Name = "btnAddCategory";
            this.btnAddCategory.Size = new System.Drawing.Size(110, 28);
            this.btnAddCategory.TabIndex = 4;
            this.btnAddCategory.Text = "Добавить категорию";
            this.btnAddCategory.UseVisualStyleBackColor = true;
            this.btnAddCategory.Click += new System.EventHandler(this.btnAddCategory_Click);
            // 
            // tabActs
            // 
            this.tabActs.Controls.Add(this.btnNonConformityInAct);
            this.tabActs.Controls.Add(this.btnAssignToAct);
            this.tabActs.Controls.Add(this.btnGenerateActQr);
            this.tabActs.Controls.Add(this.dgvUnassignedProducts);
            this.tabActs.Controls.Add(this.lblSelectProducts);
            this.tabActs.Controls.Add(this.dgvProductsInAct);
            this.tabActs.Controls.Add(this.lblProductsInAct);
            this.tabActs.Controls.Add(this.lblSelectAct);
            this.tabActs.Controls.Add(this.cmbActs);
            this.tabActs.Controls.Add(this.grpAct);
            this.tabActs.Location = new System.Drawing.Point(4, 26);
            this.tabActs.Name = "tabActs";
            this.tabActs.Padding = new System.Windows.Forms.Padding(10);
            this.tabActs.Size = new System.Drawing.Size(892, 535);
            this.tabActs.TabIndex = 1;
            this.tabActs.Text = "Акты (Отгрузка)";
            this.tabActs.UseVisualStyleBackColor = true;
            // 
            // btnNonConformityInAct
            // 
            this.btnNonConformityInAct.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNonConformityInAct.Location = new System.Drawing.Point(620, 458);
            this.btnNonConformityInAct.Name = "btnNonConformityInAct";
            this.btnNonConformityInAct.Size = new System.Drawing.Size(250, 35);
            this.btnNonConformityInAct.TabIndex = 7;
            this.btnNonConformityInAct.Text = "Ярлык несоответствия (выбранный)";
            this.btnNonConformityInAct.UseVisualStyleBackColor = true;
            this.btnNonConformityInAct.Click += new System.EventHandler(this.btnNonConformityInAct_Click);
            // 
            // btnAssignToAct
            // 
            this.btnAssignToAct.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAssignToAct.Location = new System.Drawing.Point(13, 458);
            this.btnAssignToAct.Name = "btnAssignToAct";
            this.btnAssignToAct.Size = new System.Drawing.Size(280, 35);
            this.btnAssignToAct.TabIndex = 5;
            this.btnAssignToAct.Text = "Привязать выбранные к акту";
            this.btnAssignToAct.UseVisualStyleBackColor = true;
            this.btnAssignToAct.Click += new System.EventHandler(this.btnAssignToAct_Click);
            // 
            // btnGenerateActQr
            // 
            this.btnGenerateActQr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGenerateActQr.Location = new System.Drawing.Point(310, 458);
            this.btnGenerateActQr.Name = "btnGenerateActQr";
            this.btnGenerateActQr.Size = new System.Drawing.Size(300, 35);
            this.btnGenerateActQr.TabIndex = 6;
            this.btnGenerateActQr.Text = "Генерировать QR-коды для акта";
            this.btnGenerateActQr.UseVisualStyleBackColor = true;
            this.btnGenerateActQr.Click += new System.EventHandler(this.btnGenerateActQr_Click);
            // 
            // dgvUnassignedProducts
            // 
            this.dgvUnassignedProducts.AllowUserToAddRows = false;
            this.dgvUnassignedProducts.AllowUserToDeleteRows = false;
            this.dgvUnassignedProducts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvUnassignedProducts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvUnassignedProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvUnassignedProducts.Location = new System.Drawing.Point(13, 330);
            this.dgvUnassignedProducts.Name = "dgvUnassignedProducts";
            this.dgvUnassignedProducts.ReadOnly = true;
            this.dgvUnassignedProducts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvUnassignedProducts.Size = new System.Drawing.Size(866, 117);
            this.dgvUnassignedProducts.TabIndex = 2;
            // 
            // lblSelectProducts
            // 
            this.lblSelectProducts.AutoSize = true;
            this.lblSelectProducts.Location = new System.Drawing.Point(13, 308);
            this.lblSelectProducts.Name = "lblSelectProducts";
            this.lblSelectProducts.Size = new System.Drawing.Size(713, 19);
            this.lblSelectProducts.TabIndex = 1;
            this.lblSelectProducts.Text = "Продукты без акта: выберите конкретный или несколько (клик по строке), затем нажм" +
    "ите «Привязать к акту».";
            // 
            // dgvProductsInAct
            // 
            this.dgvProductsInAct.AllowUserToAddRows = false;
            this.dgvProductsInAct.AllowUserToDeleteRows = false;
            this.dgvProductsInAct.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvProductsInAct.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvProductsInAct.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvProductsInAct.Location = new System.Drawing.Point(13, 142);
            this.dgvProductsInAct.Name = "dgvProductsInAct";
            this.dgvProductsInAct.ReadOnly = true;
            this.dgvProductsInAct.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvProductsInAct.Size = new System.Drawing.Size(866, 160);
            this.dgvProductsInAct.TabIndex = 8;
            // 
            // lblProductsInAct
            // 
            this.lblProductsInAct.AutoSize = true;
            this.lblProductsInAct.Location = new System.Drawing.Point(13, 120);
            this.lblProductsInAct.Name = "lblProductsInAct";
            this.lblProductsInAct.Size = new System.Drawing.Size(196, 19);
            this.lblProductsInAct.TabIndex = 7;
            this.lblProductsInAct.Text = "Продукты в выбранном акте:";
            // 
            // lblSelectAct
            // 
            this.lblSelectAct.AutoSize = true;
            this.lblSelectAct.Location = new System.Drawing.Point(13, 90);
            this.lblSelectAct.Name = "lblSelectAct";
            this.lblSelectAct.Size = new System.Drawing.Size(98, 19);
            this.lblSelectAct.TabIndex = 3;
            this.lblSelectAct.Text = "Выберите акт:";
            // 
            // cmbActs
            // 
            this.cmbActs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbActs.Location = new System.Drawing.Point(140, 87);
            this.cmbActs.Name = "cmbActs";
            this.cmbActs.Size = new System.Drawing.Size(250, 25);
            this.cmbActs.TabIndex = 4;
            this.cmbActs.SelectedIndexChanged += new System.EventHandler(this.cmbActs_SelectedIndexChanged);
            // 
            // grpAct
            // 
            this.grpAct.Controls.Add(this.lblActNumber);
            this.grpAct.Controls.Add(this.txtActNumber);
            this.grpAct.Controls.Add(this.btnCreateAct);
            this.grpAct.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpAct.Location = new System.Drawing.Point(10, 10);
            this.grpAct.Name = "grpAct";
            this.grpAct.Padding = new System.Windows.Forms.Padding(10);
            this.grpAct.Size = new System.Drawing.Size(872, 65);
            this.grpAct.TabIndex = 0;
            this.grpAct.TabStop = false;
            this.grpAct.Text = "Создать акт";
            // 
            // lblActNumber
            // 
            this.lblActNumber.AutoSize = true;
            this.lblActNumber.Location = new System.Drawing.Point(13, 32);
            this.lblActNumber.Name = "lblActNumber";
            this.lblActNumber.Size = new System.Drawing.Size(86, 19);
            this.lblActNumber.TabIndex = 0;
            this.lblActNumber.Text = "Номер акта:";
            // 
            // txtActNumber
            // 
            this.txtActNumber.Location = new System.Drawing.Point(110, 29);
            this.txtActNumber.MaxLength = 6;
            this.txtActNumber.Name = "txtActNumber";
            this.txtActNumber.Size = new System.Drawing.Size(200, 25);
            this.txtActNumber.TabIndex = 1;
            // 
            // btnCreateAct
            // 
            this.btnCreateAct.Location = new System.Drawing.Point(320, 27);
            this.btnCreateAct.Name = "btnCreateAct";
            this.btnCreateAct.Size = new System.Drawing.Size(150, 28);
            this.btnCreateAct.TabIndex = 2;
            this.btnCreateAct.Text = "Создать акт";
            this.btnCreateAct.UseVisualStyleBackColor = true;
            this.btnCreateAct.Click += new System.EventHandler(this.btnCreateAct_Click);
            // 
            // cmbCategory
            // 
            this.cmbCategory.Location = new System.Drawing.Point(0, 0);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(121, 21);
            this.cmbCategory.TabIndex = 0;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnLogout);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(900, 35);
            this.panelTop.TabIndex = 10;
            // 
            // btnLogout
            // 
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnLogout.Location = new System.Drawing.Point(795, 4);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(100, 28);
            this.btnLogout.TabIndex = 0;
            this.btnLogout.Text = "Выйти";
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // WarehouseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.panelTop);
            this.Name = "WarehouseForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Склад";
            this.tabControl.ResumeLayout(false);
            this.tabProducts.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvProducts)).EndInit();
            this.panelSearch.ResumeLayout(false);
            this.panelSearch.PerformLayout();
            this.grpProduct.ResumeLayout(false);
            this.grpProduct.PerformLayout();
            this.grpCategory.ResumeLayout(false);
            this.grpCategory.PerformLayout();
            this.tabActs.ResumeLayout(false);
            this.tabActs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUnassignedProducts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProductsInAct)).EndInit();
            this.grpAct.ResumeLayout(false);
            this.grpAct.PerformLayout();
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabProducts;
        private System.Windows.Forms.TabPage tabActs;

        private System.Windows.Forms.GroupBox grpCategory;
        private System.Windows.Forms.Label lblNewCategory;
        private System.Windows.Forms.TextBox txtNewCategory;
        private System.Windows.Forms.Label lblCountry;
        private System.Windows.Forms.ComboBox cmbCountry;
        private System.Windows.Forms.Button btnAddCategory;
        private System.Windows.Forms.ComboBox cmbCategory;

        private System.Windows.Forms.GroupBox grpProduct;
        private System.Windows.Forms.Label lblSerial;
        private System.Windows.Forms.TextBox txtSerial;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.ComboBox cmbProductCategory;
        private System.Windows.Forms.Button btnAddProduct;

        private System.Windows.Forms.DataGridView dgvProducts;

        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSort;
        private System.Windows.Forms.ComboBox cmbSort;
        private System.Windows.Forms.Button btnApplyFilter;
        private System.Windows.Forms.Button btnResetFilter;

        private System.Windows.Forms.GroupBox grpAct;
        private System.Windows.Forms.Label lblActNumber;
        private System.Windows.Forms.TextBox txtActNumber;
        private System.Windows.Forms.Button btnCreateAct;

        private System.Windows.Forms.Label lblSelectProducts;
        private System.Windows.Forms.DataGridView dgvUnassignedProducts;
        private System.Windows.Forms.ComboBox cmbActs;
        private System.Windows.Forms.Label lblSelectAct;
        private System.Windows.Forms.Label lblProductsInAct;
        private System.Windows.Forms.DataGridView dgvProductsInAct;
        private System.Windows.Forms.Button btnAssignToAct;
        private System.Windows.Forms.Button btnGenerateActQr;
        private System.Windows.Forms.Button btnGenerateQr;
        private System.Windows.Forms.Button btnNonConformity;
        private System.Windows.Forms.Button btnNonConformityInAct;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnLogout;
    }
}
