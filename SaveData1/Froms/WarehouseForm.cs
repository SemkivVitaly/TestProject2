using System;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    /// <summary>Форма склада: продукты, фильтрация и сортировка, привязка к актам, ярлыки несоответствия, генерация QR.</summary>
    public partial class WarehouseForm : Form
    {
        private UsersProfile _currentUser;
        private string _searchText = "";
        /// <summary>Режим сортировки: серийный номер, категория или акт.</summary>
        private int _sortMode = 0;
        /// <summary>Доступ к вкладке «Управление» при разрешении «Админ».</summary>
        private bool _hasStorageAdmin;

        public WarehouseForm(UsersProfile user)
        {
            InitializeComponent();
            _currentUser = user;
            _hasStorageAdmin = user?.Role?.RoleName == "Storage" &&
                (user.UserWithPermissions?.Any(p => p.Permissions != null && (p.Permissions.PermissionsName == "Администратор" || p.Permissions.PermissionsName == "Admin")) ?? false);
            this.Text = "Склад — " + user.UserName;
            this.Load += WarehouseForm_Load;
            this.tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private TabPage _tabAdminManagement;
        private ComboBox _cmbAdminTable;
        private DataGridView _dgvAdminData;
        private Button _btnAdminAdd, _btnAdminEdit, _btnAdminDelete;

        private Button _btnShipAct;
        private Button _btnRemoveFromAct;

        private void WarehouseForm_Load(object sender, EventArgs e)
        {
            LoadCategories();
            LoadCountriesForCategory();
            cmbSort.Items.AddRange(new object[] { "По серийному номеру", "По категории", "По акту" });
            cmbSort.SelectedIndex = 0;
            LoadProducts();
            AttachProductGridContextMenu(dgvProducts, LoadProducts);
            AttachProductGridContextMenu(dgvProductsInAct, () => { LoadProductsInAct(); LoadProducts(); });
            AttachProductGridContextMenu(dgvUnassignedProducts, () => { LoadUnassignedProducts(); LoadProducts(); });
            AttachProductGridContextMenu(dgvPostTesting, LoadPostTestingProducts);

            CreateActActionButtons();

            if (_hasStorageAdmin)
                CreateStorageAdminTab();
        }

        private void CreateActActionButtons()
        {
            _btnShipAct = new Button { Text = "Отгрузить", Left = 400, Top = 85, Width = 120, Height = 28 };
            _btnShipAct.Click += BtnShipAct_Click;
            tabActs.Controls.Add(_btnShipAct);

            _btnRemoveFromAct = new Button { Text = "Убрать из акта", Left = 530, Top = 85, Width = 140, Height = 28 };
            _btnRemoveFromAct.Click += BtnRemoveFromAct_Click;
            tabActs.Controls.Add(_btnRemoveFromAct);
        }

        private void UpdateShipButtonState()
        {
            var selectedAct = cmbActs.SelectedItem as Act;
            if (selectedAct == null || _btnShipAct == null) return;
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var act = context.Act.Find(selectedAct.ActID);
                    if (act != null && act.IsReady)
                    {
                        _btnShipAct.Text = "Вернуть";
                        _btnShipAct.Enabled = true;
                    }
                    else
                    {
                        _btnShipAct.Text = "Отгрузить";
                        _btnShipAct.Enabled = true;
                    }
                }
            }
            catch { _btnShipAct.Enabled = true; }
        }

        private void BtnShipAct_Click(object sender, EventArgs e)
        {
            var selectedAct = cmbActs.SelectedItem as Act;
            if (selectedAct == null)
            {
                MessageBox.Show("Выберите акт.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var act = context.Act.Find(selectedAct.ActID);
                    if (act == null) { MessageBox.Show("Акт не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

                    if (act.IsReady)
                    {
                        var productIdsInAct = context.Product.Where(p => p.ActID == act.ActID).Select(p => p.ProductID).ToList();
                        bool anyInWork = context.TechnicalMapFull.Any(f => productIdsInAct.Contains(f.ProductID));
                        if (anyInWork)
                        {
                            MessageBox.Show("Невозможно вернуть акт: хотя бы один продукт акта уже принят в работу (сборка/тестирование).",
                                "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (MessageBox.Show($"Вернуть акт \"{selectedAct.ActNumber}\"? Акт снова станет недоступен для сотрудников.",
                            "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            return;
                        act.IsReady = false;
                        context.SaveChanges();
                        UpdateShipButtonState();
                        MessageBox.Show("Акт возвращён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        if (MessageBox.Show($"Отгрузить акт \"{selectedAct.ActNumber}\"? После отгрузки акт станет доступен сотрудникам.",
                            "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            return;
                        act.IsReady = true;
                        context.SaveChanges();
                        UpdateShipButtonState();
                        MessageBox.Show("Акт отгружен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveFromAct_Click(object sender, EventArgs e)
        {
            if (cmbActs.SelectedItem == null)
            {
                MessageBox.Show("Выберите акт.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedAct = cmbActs.SelectedItem as Act;
            if (selectedAct != null)
            {
                try
                {
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var act = context.Act.Find(selectedAct.ActID);
                        if (act != null && act.IsReady)
                        {
                            MessageBox.Show("Из отгруженного акта нельзя убрать продукт. Сначала верните акт (кнопка «Вернуть»).",
                                "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                catch { }
            }
            if (dgvProductsInAct.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукт в таблице «Продукты в выбранном акте».", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Убрать выбранные продукты из акта?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var selectedIds = dgvProductsInAct.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.Cells["ProductID"].Value != null)
                .Select(r => (int)r.Cells["ProductID"].Value)
                .ToList();

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var products = context.Product.Where(p => selectedIds.Contains(p.ProductID)).ToList();
                    foreach (var p in products)
                        p.ActID = null;
                    context.SaveChanges();
                }
                LoadProductsInAct();
                LoadUnassignedProducts();
                LoadProducts();
                MessageBox.Show("Продукты убраны из акта.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Создание вкладки «Управление»: справочники Country, ProducType, Product.</summary>
        private void CreateStorageAdminTab()
        {
            _tabAdminManagement = new TabPage("Управление");
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
            var lblTable = new Label { Text = "Таблица:", Left = 10, Top = 10, AutoSize = true };
            _cmbAdminTable = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Left = 82, Top = 6 };
            _cmbAdminTable.Items.AddRange(new object[] { "Страны (Country)", "Категории продуктов (ProducType)", "Продукты (Product)" });
            _cmbAdminTable.SelectedIndex = 0;
            _cmbAdminTable.SelectedIndexChanged += (s, ev) => LoadAdminTableData();

            _btnAdminAdd = new Button { Text = "Добавить", Left = 318, Top = 4, Width = 90 };
            _btnAdminEdit = new Button { Text = "Изменить", Left = 416, Top = 4, Width = 90 };
            _btnAdminDelete = new Button { Text = "Удалить", Left = 514, Top = 4, Width = 90 };
            _btnAdminAdd.Click += BtnAdminAdd_Click;
            _btnAdminEdit.Click += BtnAdminEdit_Click;
            _btnAdminDelete.Click += BtnAdminDelete_Click;

            panelTop.Controls.Add(lblTable);
            panelTop.Controls.Add(_cmbAdminTable);
            panelTop.Controls.Add(_btnAdminAdd);
            panelTop.Controls.Add(_btnAdminEdit);
            panelTop.Controls.Add(_btnAdminDelete);

            _dgvAdminData = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _tabAdminManagement.Controls.Add(_dgvAdminData);
            _tabAdminManagement.Controls.Add(panelTop);
            tabControl.Controls.Add(_tabAdminManagement);
            LoadAdminTableData();
        }

        private void LoadAdminTableData()
        {
            if (_dgvAdminData == null || _cmbAdminTable?.SelectedIndex < 0) return;
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    switch (_cmbAdminTable.SelectedIndex)
                    {
                        case 0:
                            var countries = context.Country.OrderBy(c => c.CountryName).Select(c => new { c.CountryID, Страна = c.CountryName }).ToList();
                            _dgvAdminData.DataSource = countries;
                            if (_dgvAdminData.Columns.Contains("CountryID")) _dgvAdminData.Columns["CountryID"].Visible = false;
                            if (_dgvAdminData.Columns.Contains("Страна")) _dgvAdminData.Columns["Страна"].HeaderText = "Страна";
                            break;
                        case 1:
                            var types = context.ProducType.AsNoTracking().Include("Country").OrderBy(t => t.TypeName).Select(t => new { t.TypeID, Категория = t.TypeName, Страна = t.Country != null ? t.Country.CountryName : "" }).ToList();
                            _dgvAdminData.DataSource = types;
                            if (_dgvAdminData.Columns.Contains("TypeID")) _dgvAdminData.Columns["TypeID"].Visible = false;
                            if (_dgvAdminData.Columns.Contains("Категория")) _dgvAdminData.Columns["Категория"].HeaderText = "Категория";
                            if (_dgvAdminData.Columns.Contains("Страна")) _dgvAdminData.Columns["Страна"].HeaderText = "Страна";
                            break;
                        case 2:
                            var products = context.Product.AsNoTracking().Include(p => p.ProducType).Include(p => p.Act).OrderBy(p => p.ProductSerial).Select(p => new { p.ProductID, СерийныйНомер = p.ProductSerial, Категория = p.ProducType != null ? p.ProducType.TypeName : "", Акт = p.Act != null ? p.Act.ActNumber : "—" }).ToList();
                            _dgvAdminData.DataSource = products;
                            if (_dgvAdminData.Columns.Contains("ProductID")) _dgvAdminData.Columns["ProductID"].Visible = false;
                            if (_dgvAdminData.Columns.Contains("СерийныйНомер")) _dgvAdminData.Columns["СерийныйНомер"].HeaderText = "Серийный номер";
                            if (_dgvAdminData.Columns.Contains("Категория")) _dgvAdminData.Columns["Категория"].HeaderText = "Категория";
                            if (_dgvAdminData.Columns.Contains("Акт")) _dgvAdminData.Columns["Акт"].HeaderText = "Акт";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ShowInputBox(string prompt, string title, string defaultValue = "")
        {
            using (var form = new Form { FormBorderStyle = FormBorderStyle.FixedDialog, Text = title, Width = 400, Height = 120, StartPosition = FormStartPosition.CenterParent })
            {
                var lbl = new Label { Text = prompt, Left = 10, Top = 10, AutoSize = true };
                var tb = new TextBox { Left = 10, Top = 35, Width = 360, Text = defaultValue };
                var btnOk = new Button { Text = "OK", Left = 210, Top = 65, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Отмена", Left = 295, Top = 65, Width = 75, DialogResult = DialogResult.Cancel };
                form.AcceptButton = btnOk; form.CancelButton = btnCancel;
                form.Controls.AddRange(new Control[] { lbl, tb, btnOk, btnCancel });
                return form.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }

        private void BtnAdminAdd_Click(object sender, EventArgs e)
        {
            if (_cmbAdminTable.SelectedIndex == 0)
            {
                string name = ShowInputBox("Название страны:", "Новая страна", "");
                if (string.IsNullOrWhiteSpace(name)) return;
                try
                {
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        if (context.Country.Any(c => c.CountryName == name)) { MessageBox.Show("Такая страна уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        context.Country.Add(new Country { CountryName = name });
                        context.SaveChanges();
                    }
                    LoadAdminTableData(); LoadCountriesForCategory(); LoadCategories();
                    MessageBox.Show("Страна добавлена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            else if (_cmbAdminTable.SelectedIndex == 1)
            {
                string name = ShowInputBox("Название категории:", "Новая категория", "");
                string countryName = ShowInputBox("Страна производитель:", "Страна", "Россия");
                if (string.IsNullOrWhiteSpace(name)) return;
                try
                {
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var country = context.Country.FirstOrDefault(c => c.CountryName == countryName);
                        if (country == null) { country = new Country { CountryName = countryName }; context.Country.Add(country); context.SaveChanges(); }
                        if (context.ProducType.Any(t => t.TypeName == name && t.CountryID == country.CountryID)) { MessageBox.Show("Такая категория уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        context.ProducType.Add(new ProducType { TypeName = name, CountryID = country.CountryID });
                        context.SaveChanges();
                    }
                    LoadAdminTableData(); LoadCategories();
                    MessageBox.Show("Категория добавлена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            else if (_cmbAdminTable.SelectedIndex == 2)
            {
                string serial = ShowInputBox("Серийный номер продукта:", "Новый продукт", "");
                if (string.IsNullOrWhiteSpace(serial)) return;
                serial = Transliterate(serial).ToUpper();
                try
                {
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        if (context.Product.Any(p => p.ProductSerial == serial)) { MessageBox.Show("Такой продукт уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        var firstType = context.ProducType.FirstOrDefault();
                        if (firstType == null) { MessageBox.Show("Сначала добавьте категорию продуктов.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        context.Product.Add(new Product { ProductSerial = serial, TypeID = firstType.TypeID });
                        context.SaveChanges();
                    }
                    LoadAdminTableData(); LoadProducts(); LoadUnassignedProducts();
                    MessageBox.Show("Продукт добавлен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private void BtnAdminEdit_Click(object sender, EventArgs e)
        {
            if (_dgvAdminData?.SelectedRows.Count == 0) { MessageBox.Show("Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var row = _dgvAdminData.SelectedRows[0];
            try
            {
                if (_cmbAdminTable.SelectedIndex == 0)
                {
                    int id = Convert.ToInt32(row.Cells["CountryID"].Value);
                    string name = (row.Cells["Страна"].Value ?? "").ToString();
                    string newName = ShowInputBox("Название страны:", "Изменить", name);
                    if (string.IsNullOrWhiteSpace(newName) || newName == name) return;
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var c = context.Country.Find(id);
                        if (c == null) return;
                        if (context.Country.Any(x => x.CountryID != id && x.CountryName == newName)) { MessageBox.Show("Такая страна уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        c.CountryName = newName;
                        context.SaveChanges();
                    }
                    LoadAdminTableData(); LoadCountriesForCategory(); LoadCategories();
                    MessageBox.Show("Страна изменена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (_cmbAdminTable.SelectedIndex == 1)
                {
                    int id = Convert.ToInt32(row.Cells["TypeID"].Value);
                    string name = (row.Cells["Категория"].Value ?? "").ToString();
                    string newName = ShowInputBox("Название категории:", "Изменить", name);
                    if (string.IsNullOrWhiteSpace(newName) || newName == name) return;
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var t = context.ProducType.Find(id);
                        if (t == null) return;
                        if (context.ProducType.Any(x => x.TypeID != id && x.TypeName == newName && x.CountryID == t.CountryID)) { MessageBox.Show("Такая категория уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        t.TypeName = newName;
                        context.SaveChanges();
                    }
                    LoadAdminTableData(); LoadCategories();
                    MessageBox.Show("Категория изменена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (_cmbAdminTable.SelectedIndex == 2)
                {
                    int productId = Convert.ToInt32(row.Cells["ProductID"].Value);
                    string currentSerial = (row.Cells["СерийныйНомер"].Value ?? "").ToString();
                    using (var dialog = new ProductSerialEditDialog(currentSerial))
                    {
                        if (dialog.ShowDialog(this) != DialogResult.OK) return;
                        string newSerial = Transliterate(dialog.NewSerial).ToUpper();
                        if (string.IsNullOrWhiteSpace(newSerial)) return;
                        using (var context = ConnectionHelper.CreateContext())
                        {
                            var p = context.Product.Find(productId);
                            if (p == null) return;
                            if (context.Product.Any(x => x.ProductID != productId && x.ProductSerial == newSerial)) { MessageBox.Show("Такой серийный номер уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            p.ProductSerial = newSerial;
                            context.SaveChanges();
                        }
                        LoadAdminTableData(); LoadProducts(); LoadUnassignedProducts(); LoadProductsInAct();
                        MessageBox.Show("Продукт изменён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnAdminDelete_Click(object sender, EventArgs e)
        {
            if (_dgvAdminData?.SelectedRows.Count == 0) { MessageBox.Show("Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var row = _dgvAdminData.SelectedRows[0];
            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                if (_cmbAdminTable.SelectedIndex == 0)
                {
                    int id = Convert.ToInt32(row.Cells["CountryID"].Value);
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var c = context.Country.Find(id);
                        if (c != null) { context.Country.Remove(c); context.SaveChanges(); }
                    }
                    LoadAdminTableData(); LoadCountriesForCategory(); LoadCategories();
                }
                else if (_cmbAdminTable.SelectedIndex == 1)
                {
                    int id = Convert.ToInt32(row.Cells["TypeID"].Value);
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var t = context.ProducType.Find(id);
                        if (t != null) { context.ProducType.Remove(t); context.SaveChanges(); }
                    }
                    LoadAdminTableData(); LoadCategories();
                }
                else if (_cmbAdminTable.SelectedIndex == 2)
                {
                    int productId = Convert.ToInt32(row.Cells["ProductID"].Value);
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var p = context.Product.Find(productId);
                        if (p != null) { context.Product.Remove(p); context.SaveChanges(); }
                    }
                    LoadAdminTableData(); LoadProducts(); LoadUnassignedProducts(); LoadProductsInAct();
                }
                MessageBox.Show("Запись удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка (возможно, есть связанные записи): " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>Контекстное меню грида: переименование и удаление.</summary>
        private void AttachProductGridContextMenu(DataGridView dgv, System.Action refreshAfterChange)
        {
            var menu = new ContextMenuStrip();
            var miRename = new ToolStripMenuItem("Переименовать");
            var miDelete = new ToolStripMenuItem("Удалить");
            miRename.Click += (s, ev) => ProductGrid_Rename(dgv, refreshAfterChange);
            miDelete.Click += (s, ev) => ProductGrid_Delete(dgv, refreshAfterChange);
            menu.Items.Add(miRename);
            menu.Items.Add(miDelete);
            dgv.ContextMenuStrip = menu;
        }

        private void ProductGrid_Rename(DataGridView dgv, System.Action refreshAfterChange)
        {
            if (dgv.SelectedRows.Count == 0) return;
            var row = dgv.SelectedRows[0];
            if (!dgv.Columns.Contains("ProductID") || !dgv.Columns.Contains("SerialNumber")) return;
            int productId = Convert.ToInt32(row.Cells["ProductID"].Value);
            string currentSerial = (row.Cells["SerialNumber"].Value ?? "").ToString();
            using (var dialog = new ProductSerialEditDialog(currentSerial))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string newSerial = dialog.NewSerial;
                if (string.IsNullOrWhiteSpace(newSerial)) return;
                newSerial = Transliterate(newSerial).ToUpper();
                try
                {
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var product = context.Product.Find(productId);
                        if (product == null) { MessageBox.Show("Продукт не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                        if (context.Product.Any(p => p.ProductID != productId && p.ProductSerial == newSerial))
                        {
                            MessageBox.Show("Продукт с таким серийным номером уже существует.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        product.ProductSerial = newSerial;
                        context.SaveChanges();
                    }
                    refreshAfterChange();
                    MessageBox.Show("Серийный номер изменён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ProductGrid_Delete(DataGridView dgv, System.Action refreshAfterChange)
        {
            if (dgv.SelectedRows.Count == 0) return;
            var row = dgv.SelectedRows[0];
            if (!dgv.Columns.Contains("ProductID")) return;
            int productId = Convert.ToInt32(row.Cells["ProductID"].Value);
            if (MessageBox.Show("Удалить выбранный продукт из базы данных?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var product = context.Product.Find(productId);
                    if (product == null) { MessageBox.Show("Продукт не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    context.Product.Remove(product);
                    context.SaveChanges();
                }
                refreshAfterChange();
                MessageBox.Show("Продукт удалён.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления (возможно, есть связанные записи): " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Заполнение комбобокса стран производителей.</summary>
        private void LoadCountriesForCategory()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var countries = context.Country.OrderBy(c => c.CountryName).Select(c => c.CountryName).ToList();
                    cmbCountry.Items.Clear();
                    foreach (var name in countries)
                        cmbCountry.Items.Add(name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки стран: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabActs)
            {
                LoadActs();
                LoadUnassignedProducts();
                LoadProductsInAct();
            }
            else if (tabControl.SelectedTab == tabPostTesting)
            {
                LoadPostTestingProducts();
            }
        }

        private void btnPostTestingRefresh_Click(object sender, EventArgs e)
        {
            LoadPostTestingProducts();
        }

        /// <summary>Продукты, переданные на склад после тестирования и контроля.</summary>
        private void LoadPostTestingProducts()
        {
            try
            {
                string search = (txtPostTestingSearch?.Text ?? "").Trim().ToLowerInvariant();
                using (var context = ConnectionHelper.CreateContext())
                {
                    var q = context.Product.AsNoTracking()
                        .Include(p => p.ProducType)
                        .Include(p => p.Act)
                        .Where(p => p.PostTestingWarehouseAt != null);
                    if (!string.IsNullOrEmpty(search))
                    {
                        q = q.Where(p =>
                            (p.ProductSerial != null && p.ProductSerial.ToLower().Contains(search)) ||
                            (p.ProducType != null && p.ProducType.TypeName != null && p.ProducType.TypeName.ToLower().Contains(search)) ||
                            (p.Act != null && p.Act.ActNumber != null && p.Act.ActNumber.ToLower().Contains(search)));
                    }

                    var list = q.OrderByDescending(p => p.PostTestingWarehouseAt)
                        .Select(p => new
                        {
                            p.ProductID,
                            SerialNumber = p.ProductSerial,
                            Category = p.ProducType != null ? p.ProducType.TypeName : "",
                            Act = p.Act != null ? p.Act.ActNumber : "—",
                            Передано = p.PostTestingWarehouseAt
                        })
                        .ToList();

                    dgvPostTesting.DataSource = list;
                    if (dgvPostTesting.Columns.Contains("ProductID")) dgvPostTesting.Columns["ProductID"].Visible = false;
                    if (dgvPostTesting.Columns.Contains("SerialNumber")) dgvPostTesting.Columns["SerialNumber"].HeaderText = "Серийный номер";
                    if (dgvPostTesting.Columns.Contains("Category")) dgvPostTesting.Columns["Category"].HeaderText = "Категория";
                    if (dgvPostTesting.Columns.Contains("Act")) dgvPostTesting.Columns["Act"].HeaderText = "Акт";
                    if (dgvPostTesting.Columns.Contains("Передано")) dgvPostTesting.Columns["Передано"].HeaderText = "Передано на склад (UTC)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbActs_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadProductsInAct();
            UpdateShipButtonState();
        }

        /// <summary>Загрузка продуктов выбранного акта в грид.</summary>
        private void LoadProductsInAct()
        {
            var selectedAct = cmbActs.SelectedItem as Act;
            if (selectedAct == null)
            {
                dgvProductsInAct.DataSource = null;
                return;
            }
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var products = context.Product
                        .Include(p => p.ProducType)
                        .Where(p => p.Act != null && p.Act.ActNumber == selectedAct.ActNumber)
                        .Select(p => new
                        {
                            p.ProductID,
                            SerialNumber = p.ProductSerial,
                            Category = p.ProducType != null ? p.ProducType.TypeName : ""
                        })
                        .ToList();

                    dgvProductsInAct.DataSource = products;
                    if (dgvProductsInAct.Columns.Contains("SerialNumber")) dgvProductsInAct.Columns["SerialNumber"].HeaderText = "Серийный номер";
                    if (dgvProductsInAct.Columns.Contains("Category")) dgvProductsInAct.Columns["Category"].HeaderText = "Категория";
                    if (dgvProductsInAct.Columns.Contains("ProductID"))
                        dgvProductsInAct.Columns["ProductID"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов акта: " + ExceptionDisplay.MessageWithInners(ex), "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            _searchText = txtSearch.Text ?? "";
            _sortMode = cmbSort.SelectedIndex >= 0 ? cmbSort.SelectedIndex : 0;
            LoadProducts();
        }

        /// <summary>Замена кириллицы на латиницу при выходе из поля поиска.</summary>
        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
            txtSearch.Text = TransliterateOnlyCyrillic(txtSearch.Text);
        }

        /// <summary>Транслитерация кириллицы в латиницу с сохранением регистра.</summary>
        private string TransliterateOnlyCyrillic(string text)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (IsCyrillic(c))
                {
                    string lat = Transliterate(c.ToString());
                    if (char.IsUpper(c)) lat = lat.ToUpper();
                    sb.Append(lat);
                }
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Мгновенная транслитерация кириллицы при вводе.</summary>
        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!IsCyrillic(e.KeyChar)) return;
            e.Handled = true;
            var tb = (TextBox)sender;
            string converted = Transliterate(e.KeyChar.ToString());
            if (char.IsUpper(e.KeyChar)) converted = converted.ToUpper();
            int start = tb.SelectionStart;
            int len = tb.SelectionLength;
            tb.Text = tb.Text.Remove(start, len).Insert(start, converted);
            tb.SelectionStart = start + converted.Length;
        }

        private static bool IsCyrillic(char c)
        {
            return (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'ё' || c == 'Ё';
        }

        private void btnResetFilter_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            _searchText = "";
            cmbSort.SelectedIndex = 0;
            _sortMode = 0;
            LoadProducts();
        }

        /// <summary>Заполнение комбобокса категорий продукта (тип + страна).</summary>
        private void LoadCategories()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var categories = context.ProducType.AsNoTracking().Include("Country").ToList();
                    var categoryDisplay = categories.Select(c => new
                    {
                        c.TypeID,
                        DisplayName = c.Country != null ? $"{c.TypeName} ({c.Country.CountryName})" : c.TypeName
                    }).ToList();
                    
                    cmbProductCategory.DataSource = categoryDisplay;
                    cmbProductCategory.DisplayMember = "DisplayName";
                    cmbProductCategory.ValueMember = "TypeID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Загрузка списка продуктов с фильтром и сортировкой.</summary>
        private void LoadProducts()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var query = context.Product
                        .AsNoTracking()
                        .Include(p => p.ProducType)
                        .Include(p => p.Act)
                        .Where(p => p.Act == null || !p.Act.IsReady)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(_searchText))
                    {
                        var search = _searchText.Trim().ToLower();
                        query = query.Where(p =>
                            (p.ProductSerial != null && p.ProductSerial.ToLower().Contains(search)) ||
                            (p.ProducType != null && p.ProducType.TypeName != null && p.ProducType.TypeName.ToLower().Contains(search)) ||
                            (p.Act != null && p.Act.ActNumber != null && p.Act.ActNumber.ToLower().Contains(search)));
                    }

                    var projected = query.Select(p => new
                    {
                        p.ProductID,
                        SerialNumber = p.ProductSerial,
                        Category = p.ProducType != null ? p.ProducType.TypeName : "",
                        Act = p.Act != null ? p.Act.ActNumber : "—"
                    });

                    if (_sortMode == 0)
                        projected = projected.OrderBy(p => p.SerialNumber);
                    else if (_sortMode == 1)
                        projected = projected.OrderBy(p => p.Category);
                    else
                        projected = projected.OrderBy(p => p.Act);

                    var products = projected.ToList();
                    dgvProducts.DataSource = products;
                    if (dgvProducts.Columns.Contains("ProductID"))
                        dgvProducts.Columns["ProductID"].Visible = false;
                    if (dgvProducts.Columns.Contains("SerialNumber")) dgvProducts.Columns["SerialNumber"].HeaderText = "Серийный номер";
                    if (dgvProducts.Columns.Contains("Category")) dgvProducts.Columns["Category"].HeaderText = "Категория";
                    if (dgvProducts.Columns.Contains("Act")) dgvProducts.Columns["Act"].HeaderText = "Акт";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ExceptionDisplay.MessageWithInners(ex), "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Загрузка списка актов на вкладке «Акты».</summary>
        private void LoadActs()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var acts = context.Act.ToList();
                    cmbActs.DataSource = acts;
                    cmbActs.DisplayMember = "ActNumber";
                    cmbActs.ValueMember = "ActID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки актов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Загрузка продуктов без акта для привязки к акту.</summary>
        private void LoadUnassignedProducts()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var products = context.Product
                        .Include(p => p.ProducType)
                        .Where(p => p.Act == null)
                        .Select(p => new
                        {
                            p.ProductID,
                            SerialNumber = p.ProductSerial,
                            Category = p.ProducType.TypeName
                        })
                        .ToList();

                    dgvUnassignedProducts.DataSource = products;
                    dgvUnassignedProducts.Columns["ProductID"].Visible = false;
                    if (dgvUnassignedProducts.Columns.Contains("SerialNumber")) dgvUnassignedProducts.Columns["SerialNumber"].HeaderText = "Серийный номер";
                    if (dgvUnassignedProducts.Columns.Contains("Category")) dgvUnassignedProducts.Columns["Category"].HeaderText = "Категория";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ExceptionDisplay.MessageWithInners(ex), "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddCategory_Click(object sender, EventArgs e)
        {
            string name = txtNewCategory.Text.Trim();
            string countryName = (cmbCountry.SelectedItem != null)
                ? cmbCountry.SelectedItem.ToString()
                : cmbCountry.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название категории", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(countryName))
            {
                MessageBox.Show("Выберите или введите страну производителя", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var country = context.Country.FirstOrDefault(c => c.CountryName == countryName);
                    if (country == null)
                    {
                        country = new Country { CountryName = countryName };
                        context.Country.Add(country);
                        context.SaveChanges();
                    }

                    if (context.ProducType.Any(t => t.TypeName == name && t.CountryID == country.CountryID))
                    {
                        MessageBox.Show("Категория с таким названием и страной производителем уже существует.", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    context.ProducType.Add(new ProducType { TypeName = name, CountryID = country.CountryID });
                    context.SaveChanges();
                }

                txtNewCategory.Clear();
                LoadCategories();
                MessageBox.Show("Категория добавлена", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления категории: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            string serial = txtSerial.Text.Trim();
            if (string.IsNullOrEmpty(serial))
            {
                MessageBox.Show("Введите серийный номер", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            serial = Transliterate(serial).ToUpper();

            if (cmbProductCategory.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int typeId = (int)cmbProductCategory.SelectedValue;

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var product = new Product
                    {
                        ProductSerial = serial,
                        TypeID = typeId
                    };
                    context.Product.Add(product);
                    context.SaveChanges();
                }

                txtSerial.Clear();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления продукта: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Транслитерация серийного номера: кириллица → латиница.</summary>
        private string Transliterate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder(text.Length * 2);
            foreach (char c in text.ToLower())
            {
                switch (c)
                {
                    case 'а': sb.Append("a"); break;
                    case 'б': sb.Append("b"); break;
                    case 'в': sb.Append("v"); break;
                    case 'г': sb.Append("g"); break;
                    case 'д': sb.Append("d"); break;
                    case 'е': sb.Append("e"); break;
                    case 'ё': sb.Append("e"); break;
                    case 'ж': sb.Append("zh"); break;
                    case 'з': sb.Append("z"); break;
                    case 'и': sb.Append("i"); break;
                    case 'й': sb.Append("y"); break;
                    case 'к': sb.Append("k"); break;
                    case 'л': sb.Append("l"); break;
                    case 'м': sb.Append("m"); break;
                    case 'н': sb.Append("n"); break;
                    case 'о': sb.Append("o"); break;
                    case 'п': sb.Append("p"); break;
                    case 'р': sb.Append("r"); break;
                    case 'с': sb.Append("s"); break;
                    case 'т': sb.Append("t"); break;
                    case 'у': sb.Append("u"); break;
                    case 'ф': sb.Append("f"); break;
                    case 'х': sb.Append("h"); break;
                    case 'ц': sb.Append("ts"); break;
                    case 'ч': sb.Append("ch"); break;
                    case 'ш': sb.Append("sh"); break;
                    case 'щ': sb.Append("sch"); break;
                    case 'ъ': break;
                    case 'ы': sb.Append("y"); break;
                    case 'ь': break;
                    case 'э': sb.Append("e"); break;
                    case 'ю': sb.Append("yu"); break;
                    case 'я': sb.Append("ya"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private void txtSerial_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSerial.Text)) return;
            txtSerial.Text = Transliterate(txtSerial.Text).ToUpper();
        }

        private void btnCreateAct_Click(object sender, EventArgs e)
        {
            string actNumber = txtActNumber.Text.Trim();
            if (string.IsNullOrEmpty(actNumber))
            {
                MessageBox.Show("Введите номер акта", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    if (context.Act.Any<Act>(a => a.ActNumber == actNumber))
                    {
                        MessageBox.Show("Акт с таким номером уже существует", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    context.Act.Add(new Act { ActNumber = actNumber, IsReady = false });
                    context.SaveChanges();
                }

                txtActNumber.Clear();
                LoadActs();
                MessageBox.Show("Акт создан", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка создания акта: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Создание ярлыка несоответствия для продукта на вкладке «Продукты».</summary>
        private void btnNonConformity_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукт в таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgvProducts.SelectedRows[0];
            int productId = (int)row.Cells["ProductID"].Value;
            string serial = row.Cells["SerialNumber"].Value?.ToString() ?? "";
            string category = row.Cells["Category"].Value?.ToString() ?? "";
            string actNumber = row.Cells["Act"].Value?.ToString() ?? "";

            using (var form = new NonConformityForm(productId, serial, category, actNumber,
                _currentUser.UserName ?? "", true))
            {
                form.ShowDialog(this);
            }
        }

        /// <summary>Создание ярлыка несоответствия для продукта в акте.</summary>
        private void btnNonConformityInAct_Click(object sender, EventArgs e)
        {
            if (cmbActs.SelectedItem == null)
            {
                MessageBox.Show("Выберите акт.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (dgvProductsInAct.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукт в таблице «Продукты в выбранном акте».", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedAct = cmbActs.SelectedItem as Act;
            string actNumber = selectedAct?.ActNumber ?? "";
            var row = dgvProductsInAct.SelectedRows[0];
            int productId = (int)row.Cells["ProductID"].Value;
            string serial = row.Cells["SerialNumber"].Value?.ToString() ?? "";
            string category = row.Cells["Category"].Value?.ToString() ?? "";

            using (var form = new NonConformityForm(productId, serial, category, actNumber,
                _currentUser.UserName ?? "", true))
            {
                form.ShowDialog(this);
            }
        }

        private void btnGenerateQr_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукт в таблице для генерации QR-кода", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string serial = dgvProducts.SelectedRows[0].Cells["SerialNumber"].Value?.ToString();
            if (string.IsNullOrEmpty(serial))
            {
                MessageBox.Show("У выбранного продукта нет серийного номера", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (Bitmap qrImage = QrCodeHelper.GenerateStyledQrCode(serial))
                {
                    using (var preview = new Form())
                    {
                        preview.Text = "QR-код: " + serial;
                        preview.StartPosition = FormStartPosition.CenterParent;
                        preview.Size = new System.Drawing.Size(500, 580);
                        preview.FormBorderStyle = FormBorderStyle.FixedDialog;
                        preview.MaximizeBox = false;
                        preview.MinimizeBox = false;

                        var pictureBox = new PictureBox
                        {
                            Image = new Bitmap(qrImage),
                            SizeMode = PictureBoxSizeMode.Zoom,
                            Dock = DockStyle.Fill
                        };

                        var btnSave = new Button
                        {
                            Text = "Сохранить QR-код",
                            Dock = DockStyle.Bottom,
                            Height = 40,
                            Font = new Font("Segoe UI", 10F)
                        };

                        btnSave.Click += (s, args) =>
                        {
                            using (var sfd = new SaveFileDialog())
                            {
                                sfd.Filter = "PNG изображение|*.png";
                                sfd.FileName = "QR_" + serial + ".png";
                                if (sfd.ShowDialog() == DialogResult.OK)
                                {
                                    pictureBox.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                                    MessageBox.Show("QR-код сохранён: " + sfd.FileName, "Успех",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        };

                        preview.Controls.Add(pictureBox);
                        preview.Controls.Add(btnSave);
                        preview.ShowDialog(this);

                        pictureBox.Image?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка генерации QR-кода: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGenerateActQr_Click(object sender, EventArgs e)
        {
            if (cmbActs.SelectedItem == null)
            {
                MessageBox.Show("Выберите акт для генерации QR-кодов", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedAct = cmbActs.SelectedItem as Act;
            if (selectedAct == null) return;

            string actNumber = selectedAct.ActNumber;

            if (!QrActWordDocumentService.TryEnsureQrTemplateExists(this))
                return;

            string templatePath = QrActWordDocumentService.GetTemplateFullPath();
            string savePath = QrActWordDocumentService.PromptQrOutputPath(this, actNumber);
            if (savePath == null)
                return;

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var products = context.Product
                        .Where(p => p.Act != null && p.Act.ActNumber == actNumber)
                        .ToList();

                    if (products.Count == 0)
                    {
                        MessageBox.Show("В акте \"" + actNumber + "\" нет продуктов", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    this.Cursor = Cursors.WaitCursor;
                    int generated = QrActWordDocumentService.GenerateActQrWordDocument(templatePath, savePath, actNumber, products);
                    this.Cursor = Cursors.Default;

                    MessageBox.Show(
                        "Сгенерировано QR-кодов: " + generated + " из " + products.Count + "\n" +
                        "Файл сохранен: " + savePath,
                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try
                    {
                        System.Diagnostics.Process.Start(savePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось автоматически открыть файл: " + ex.Message, "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show("Ошибка генерации документа: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void btnAssignToAct_Click(object sender, EventArgs e)
        {
            if (cmbActs.SelectedItem == null)
            {
                MessageBox.Show("Выберите акт", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvUnassignedProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукты для привязки к акту", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedAct = cmbActs.SelectedItem as Act;
            if (selectedAct == null) return;
            string actNumber = selectedAct.ActNumber;
            var selectedProductIds = dgvUnassignedProducts.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => (int)r.Cells["ProductID"].Value)
                .ToList();

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var products = context.Product
                        .Where(p => selectedProductIds.Contains(p.ProductID))
                        .ToList();

                    var act = context.Act.FirstOrDefault<Act>(a => a.ActNumber == actNumber);
                    if (act == null) { MessageBox.Show("Акт не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                    foreach (var product in products)
                    {
                        product.Act = act;
                    }
                    context.SaveChanges();
                }

                LoadUnassignedProducts();
                LoadProducts();
                LoadProductsInAct();
                MessageBox.Show("Продукты привязаны к акту", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка привязки продуктов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
