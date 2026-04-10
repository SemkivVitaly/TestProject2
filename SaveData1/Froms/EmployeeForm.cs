using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SaveData1.CrossPlateTesting.Services;
using SaveData1.Entity;
using SaveData1.Froms;
using SaveData1.Helpers;
using Excel = Microsoft.Office.Interop.Excel;

namespace SaveData1
{
    /// <summary>Режим работы формы сотрудника: сборка, тестирование или инспекция.</summary>
    public enum WorkMode { Assembly, Testing, Inspection, PolletnikAutoTesting, CrossPlataAutoTesting }

    /// <summary>Форма сотрудника: продукты по актам, сборка/тест/инспекция, экспорт в Excel, администрирование по правам.</summary>
    public partial class EmployeeForm : Form
    {
        private UsersProfile _currentUser;
        private bool _isAdmin;
        /// <summary>Доступ к администрированию по праву «Администратора».</summary>
        private bool _hasAdminPermission;
        private bool _hasAssembly;
        private bool _hasTesting;
        private bool _hasInspection;
        private WorkMode _currentWorkMode;
        /// <summary>Порядок вкладок определяет режим работы по SelectedIndex.</summary>
        private List<WorkMode> _visibleModes = new List<WorkMode>();
        /// <summary>Вкладки сборка/тест/инспекция по правам (без учёта типа продуктов в акте).</summary>
        private List<WorkMode> _baselineWorkModes = new List<WorkMode>();
        private TabPage _tabPagePolletnikAutoTest;
        private TabPage _tabPageCrossAutoTest;
        private const string PolletnikiTypeName = "Полетники";

        private enum ActProductTestingKind { Standard, Polletniki, CrossPlata }
        /// <summary>Кэш записей техкарты и продуктов для отображения в гриде.</summary>
        private List<TechnicalMapView> _allProductData = new List<TechnicalMapView>();
        private Dictionary<string, string> _pathOverrides = new Dictionary<string, string>();
        private bool _isLoadingPath = false;

        private Panel _panelActFilter;
        private ComboBox _cmbActCategory;
        private CheckBox _chkActByManufacturer;

        /// <summary>Одна строка техкарты для грида (сборка, тест или инспекция).</summary>
        public class TechnicalMapView
        {
            public int TMID { get; set; }
            /// <summary>Идентификатор техкарты (TechnicalMapFull.TMID) для проверки «После инспекции».</summary>
            public int FullTMID { get; set; }
            public int ProductID { get; set; }
            public string SerialNumber { get; set; }
            public string Category { get; set; }
            public string Act { get; set; }
            public string FullName { get; set; }
            public string Manufacturer { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan TimeStart { get; set; }
            public TimeSpan TimeEnd { get; set; }
            public bool InProgress { get; set; }
            public bool IsReady { get; set; }
            public int UserID { get; set; }
            public bool Inspection { get; set; }
            public bool AfterInspection { get; set; }
            /// <summary>Режим тестирования: дата/время и статус.</summary>
            public bool IsTesterView { get; set; }
            /// <summary>Режим инспекции: инспектор, дата и результат.</summary>
            public string InspectorName { get; set; }
            public string InspectionDateStr { get; set; }
            public string ResultText { get; set; }
        }

        public EmployeeForm(UsersProfile user)
        {
            InitializeComponent();
            _currentUser = user;
            _isAdmin = user.Role != null && user.Role.RoleName == "Admin";

            var perms = user.UserWithPermissions ?? new List<UserWithPermissions>();
            _hasAdminPermission = perms.Any(p => p.Permissions != null && (p.Permissions.PermissionsName == "Администратор"));
            _hasAssembly = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Сборщик");
            _hasTesting = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Тестировщик");
            _hasInspection = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Инспектор");

            this.Text = "Сотрудник — " + user.UserName;

            if (!_hasAdminPermission)
            {
                mainTabControl.TabPages.Remove(tabAdmin);
            }
            else
            {
                if (!_isAdmin)
                    adminTabControl.TabPages.Remove(tabAdminUsers);
            }

            if (!_hasAdminPermission)
                tabControlActions.TabPages.Remove(tabPageActionsAdmin);
            if (!_hasTesting)
                tabControlActions.TabPages.Remove(tabPageActionsTester);

            _visibleModes.Clear();
            if (_hasAssembly) _visibleModes.Add(WorkMode.Assembly);
            if (_hasTesting) _visibleModes.Add(WorkMode.Testing);
            if (_hasInspection) _visibleModes.Add(WorkMode.Inspection);

            if (_visibleModes.Count == 0)
                _visibleModes.Add(WorkMode.Assembly);

            var toRemove = new List<System.Windows.Forms.TabPage>();
            if (!_visibleModes.Contains(WorkMode.Assembly)) toRemove.Add(tabPageAssembly);
            if (!_visibleModes.Contains(WorkMode.Testing)) toRemove.Add(tabPageTesting);
            if (!_visibleModes.Contains(WorkMode.Inspection)) toRemove.Add(tabPageInspection);
            foreach (var p in toRemove)
                tabControlWork.TabPages.Remove(p);

            _baselineWorkModes = new List<WorkMode>(_visibleModes);

            _tabPagePolletnikAutoTest = new TabPage("Тестирование полетников")
            {
                Name = "tabPagePolletnikAutoTest",
                UseVisualStyleBackColor = true,
                Padding = new Padding(3)
            };
            _tabPageCrossAutoTest = new TabPage("Тестирование кросс-плат")
            {
                Name = "tabPageCrossAutoTest",
                UseVisualStyleBackColor = true,
                Padding = new Padding(3)
            };

            if (_visibleModes.Count == 1)
                tabControlWork.Visible = false;
            else
                tabControlWork.SelectedIndex = 0;

            _currentWorkMode = _visibleModes[tabControlWork.Visible ? tabControlWork.SelectedIndex : 0];

            _panelActFilter = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(5) };
            _chkActByManufacturer = new CheckBox { Text = "Учитывать производителя", Location = new System.Drawing.Point(5, 5), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 9F) };
            _cmbActCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new System.Drawing.Point(5, 32), Width = 200, Font = new System.Drawing.Font("Segoe UI", 9F) };
            _cmbActCategory.SelectedIndexChanged += (s, ev) => { LoadActs(); };
            _chkActByManufacturer.CheckedChanged += (s, ev) => { LoadActCategories(); LoadActs(); };
            _panelActFilter.Controls.Add(_chkActByManufacturer);
            _panelActFilter.Controls.Add(_cmbActCategory);
            splitContainer.Panel1.Controls.Add(_panelActFilter);

            this.Load += EmployeeForm_Load;
        }

        private void tabControlWork_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!tabControlWork.Visible || tabControlWork.SelectedIndex < 0 || tabControlWork.SelectedIndex >= _visibleModes.Count) return;
            _currentWorkMode = _visibleModes[tabControlWork.SelectedIndex];
            if (_hasAdminPermission)
                btnChangeStatus.Visible = true;
            LoadProductsForSelectedAct();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EmployeeForm_Load(object sender, EventArgs e)
        {
            LoadActCategories();
            LoadActs();
            if (_hasAdminPermission)
            {
                btnChangeStatus.Visible = true;
                LoadNoActProducts();
                LoadAdminCountries();
                LoadAdminProductTypes();
                LoadAdminActs();
                LoadAdminUnassignedProducts();
                LoadDefectStatistics();
                AttachProductGridContextMenu(dgvNoActProducts, () => { LoadNoActProducts(); LoadAdminUnassignedProducts(); });
                AttachProductGridContextMenu(dgvAdminUnassigned, () => { LoadAdminUnassignedProducts(); LoadNoActProducts(); });
                if (_isAdmin)
                {
                    LoadUsers();
                    CreateAdminDataManagementTab();
                }
            }
        }

        private TabPage _tabAdminData;
        private ComboBox _cmbAdminTable;
        private DataGridView _dgvAdminData;
        private Button _btnAdminDataAdd, _btnAdminDataEdit, _btnAdminDataDelete;

        /// <summary>Вкладка «Управление данными»: справочники БД.</summary>
        private void CreateAdminDataManagementTab()
        {
            _tabAdminData = new TabPage("Управление данными");
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(5) };
            var lblTable = new Label { Text = "Таблица:", Left = 10, Top = 12, AutoSize = true };
            _cmbAdminTable = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, Left = 82, Top = 8 };
            _cmbAdminTable.Items.AddRange(new object[] {
                "Страны (Country)", "Категории (ProducType)", "Акты (Act)", "Описания (Description)",
                "Места (Place)", "Результаты (ResultTable)", "Роли (Role)", "Разрешения (Permissions)"
            });
            _cmbAdminTable.SelectedIndex = 0;
            _cmbAdminTable.SelectedIndexChanged += (s, ev) => LoadAdminDataTable();

            _btnAdminDataAdd = new Button { Text = "Добавить", Left = 358, Top = 6, Width = 85 };
            _btnAdminDataEdit = new Button { Text = "Изменить", Left = 451, Top = 6, Width = 85 };
            _btnAdminDataDelete = new Button { Text = "Удалить", Left = 544, Top = 6, Width = 85 };
            _btnAdminDataAdd.Click += AdminDataAdd_Click;
            _btnAdminDataEdit.Click += AdminDataEdit_Click;
            _btnAdminDataDelete.Click += AdminDataDelete_Click;

            panelTop.Controls.Add(lblTable);
            panelTop.Controls.Add(_cmbAdminTable);
            panelTop.Controls.Add(_btnAdminDataAdd);
            panelTop.Controls.Add(_btnAdminDataEdit);
            panelTop.Controls.Add(_btnAdminDataDelete);

            _dgvAdminData = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _tabAdminData.Controls.Add(_dgvAdminData);
            _tabAdminData.Controls.Add(panelTop);
            adminTabControl.Controls.Add(_tabAdminData);
            LoadAdminDataTable();
        }

        private void LoadAdminDataTable()
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
                            SetColumnHeader(_dgvAdminData, "CountryID", false); SetColumnHeader(_dgvAdminData, "Страна", "Страна");
                            break;
                        case 1:
                            var types = context.ProducType.Include("Country").OrderBy(t => t.TypeName).Select(t => new { t.TypeID, Категория = t.TypeName, Страна = t.Country != null ? t.Country.CountryName : "" }).ToList();
                            _dgvAdminData.DataSource = types;
                            SetColumnHeader(_dgvAdminData, "TypeID", false); SetColumnHeader(_dgvAdminData, "Категория", "Категория"); SetColumnHeader(_dgvAdminData, "Страна", "Страна");
                            break;
                        case 2:
                            var acts = context.Act.OrderBy(a => a.ActNumber).Select(a => new { a.ActID, Номер = a.ActNumber }).ToList();
                            _dgvAdminData.DataSource = acts;
                            SetColumnHeader(_dgvAdminData, "ActID", false); SetColumnHeader(_dgvAdminData, "Номер", "Номер акта");
                            break;
                        case 3:
                            var descs = context.Description.OrderBy(d => d.DescriptionID).Select(d => new { d.DescriptionID, Текст = d.DescriptionText }).ToList();
                            _dgvAdminData.DataSource = descs;
                            SetColumnHeader(_dgvAdminData, "DescriptionID", false); SetColumnHeader(_dgvAdminData, "Текст", "Текст описания");
                            break;
                        case 4:
                            var places = context.Place.OrderBy(p => p.PlaceName).Select(p => new { p.PlaceID, Название = p.PlaceName }).ToList();
                            _dgvAdminData.DataSource = places;
                            SetColumnHeader(_dgvAdminData, "PlaceID", false); SetColumnHeader(_dgvAdminData, "Название", "Место");
                            break;
                        case 5:
                            var results = context.ResultTable.OrderBy(r => r.ResultText).Select(r => new { r.ResultID, Текст = r.ResultText }).ToList();
                            _dgvAdminData.DataSource = results;
                            SetColumnHeader(_dgvAdminData, "ResultID", false); SetColumnHeader(_dgvAdminData, "Текст", "Результат");
                            break;
                        case 6:
                            var roles = context.Role.OrderBy(r => r.RoleName).Select(r => new { r.RoleID, Роль = r.RoleName }).ToList();
                            _dgvAdminData.DataSource = roles;
                            SetColumnHeader(_dgvAdminData, "RoleID", false); SetColumnHeader(_dgvAdminData, "Роль", "Роль");
                            break;
                        case 7:
                            var perms = context.Permissions.OrderBy(p => p.PermissionsName).Select(p => new { p.PermissionsID, Разрешение = p.PermissionsName }).ToList();
                            _dgvAdminData.DataSource = perms;
                            SetColumnHeader(_dgvAdminData, "PermissionsID", false); SetColumnHeader(_dgvAdminData, "Разрешение", "Разрешение");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void SetColumnHeader(DataGridView dgv, string colName, bool visible)
        {
            if (dgv.Columns.Contains(colName)) dgv.Columns[colName].Visible = visible;
        }
        private static void SetColumnHeader(DataGridView dgv, string colName, string headerText)
        {
            if (dgv.Columns.Contains(colName)) dgv.Columns[colName].HeaderText = headerText;
        }

        private static string AdminInputBox(string prompt, string title, string defaultValue = "")
        {
            using (var form = new Form { FormBorderStyle = FormBorderStyle.FixedDialog, Text = title, Width = 420, Height = 125, StartPosition = FormStartPosition.CenterParent })
            {
                var lbl = new Label { Text = prompt, Left = 10, Top = 10, AutoSize = true };
                var tb = new TextBox { Left = 10, Top = 35, Width = 380, Text = defaultValue };
                var btnOk = new Button { Text = "OK", Left = 230, Top = 70, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Отмена", Left = 315, Top = 70, Width = 75, DialogResult = DialogResult.Cancel };
                form.AcceptButton = btnOk; form.CancelButton = btnCancel;
                form.Controls.AddRange(new Control[] { lbl, tb, btnOk, btnCancel });
                return form.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
            }
        }

        private void AdminDataAdd_Click(object sender, EventArgs e)
        {
            if (_dgvAdminData == null || _cmbAdminTable.SelectedIndex < 0) return;
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    switch (_cmbAdminTable.SelectedIndex)
                    {
                        case 0:
                            var name0 = AdminInputBox("Название страны:", "Новая страна", "");
                            if (string.IsNullOrEmpty(name0)) return;
                            if (context.Country.Any(c => c.CountryName == name0)) { MessageBox.Show("Такая страна уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            context.Country.Add(new Country { CountryName = name0 });
                            context.SaveChanges();
                            break;
                        case 1:
                            var name1 = AdminInputBox("Название категории:", "Новая категория", "");
                            var countryName = AdminInputBox("Страна:", "Страна", "Россия");
                            if (string.IsNullOrEmpty(name1)) return;
                            var country = context.Country.FirstOrDefault(c => c.CountryName == countryName);
                            if (country == null) { country = new Country { CountryName = countryName }; context.Country.Add(country); context.SaveChanges(); }
                            if (context.ProducType.Any(t => t.TypeName == name1 && t.CountryID == country.CountryID)) { MessageBox.Show("Такая категория уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            context.ProducType.Add(new ProducType { TypeName = name1, CountryID = country.CountryID });
                            context.SaveChanges();
                            break;
                        case 2:
                            var actNum = AdminInputBox("Номер акта:", "Новый акт", "");
                            if (string.IsNullOrEmpty(actNum)) return;
                            if (context.Act.Any(a => a.ActNumber == actNum)) { MessageBox.Show("Такой акт уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            context.Act.Add(new Act { ActNumber = actNum, IsReady = true });
                            context.SaveChanges();
                            break;
                        case 3:
                            var descText = AdminInputBox("Текст описания:", "Новое описание", "");
                            if (string.IsNullOrEmpty(descText)) return;
                            context.Description.Add(new Description { DescriptionText = descText });
                            context.SaveChanges();
                            break;
                        case 4:
                            var placeName = AdminInputBox("Название места:", "Новое место", "");
                            if (string.IsNullOrEmpty(placeName)) return;
                            if (context.Place.Any(p => p.PlaceName == placeName)) { MessageBox.Show("Такое место уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            context.Place.Add(new Place { PlaceName = placeName });
                            context.SaveChanges();
                            break;
                        case 5:
                            var resText = AdminInputBox("Текст результата:", "Новый результат", "");
                            if (string.IsNullOrEmpty(resText)) return;
                            context.ResultTable.Add(new ResultTable { ResultText = resText });
                            context.SaveChanges();
                            break;
                        case 6:
                            var roleName = AdminInputBox("Название роли:", "Новая роль", "");
                            if (string.IsNullOrEmpty(roleName)) return;
                            if (context.Role.Any(r => r.RoleName == roleName)) { MessageBox.Show("Такая роль уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            context.Role.Add(new Role { RoleName = roleName });
                            context.SaveChanges();
                            break;
                        case 7:
                            var permName = AdminInputBox("Название разрешения:", "Новое разрешение", "");
                            if (string.IsNullOrEmpty(permName)) return;
                            if (context.Permissions.Any(p => p.PermissionsName == permName)) { MessageBox.Show("Такое разрешение уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            context.Permissions.Add(new Permissions { PermissionsName = permName });
                            context.SaveChanges();
                            break;
                    }
                }
                LoadAdminDataTable();
                if (_cmbAdminTable.SelectedIndex == 0 || _cmbAdminTable.SelectedIndex == 1) { LoadAdminCountries(); LoadAdminProductTypes(); }
                if (_cmbAdminTable.SelectedIndex == 2) LoadAdminActs();
                MessageBox.Show("Запись добавлена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void AdminDataEdit_Click(object sender, EventArgs e)
        {
            if (_dgvAdminData?.SelectedRows.Count == 0) { MessageBox.Show("Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var row = _dgvAdminData.SelectedRows[0];
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    switch (_cmbAdminTable.SelectedIndex)
                    {
                        case 0:
                            int cid = Convert.ToInt32(row.Cells["CountryID"].Value);
                            var c = context.Country.Find(cid);
                            if (c == null) return;
                            var newName0 = AdminInputBox("Название страны:", "Изменить", c.CountryName);
                            if (string.IsNullOrEmpty(newName0) || newName0 == c.CountryName) return;
                            if (context.Country.Any(x => x.CountryID != cid && x.CountryName == newName0)) { MessageBox.Show("Такая страна уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            c.CountryName = newName0;
                            context.SaveChanges();
                            break;
                        case 1:
                            int tid = Convert.ToInt32(row.Cells["TypeID"].Value);
                            var t = context.ProducType.Find(tid);
                            if (t == null) return;
                            var newName1 = AdminInputBox("Название категории:", "Изменить", t.TypeName);
                            if (string.IsNullOrEmpty(newName1) || newName1 == t.TypeName) return;
                            if (context.ProducType.Any(x => x.TypeID != tid && x.TypeName == newName1 && x.CountryID == t.CountryID)) { MessageBox.Show("Такая категория уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            t.TypeName = newName1;
                            context.SaveChanges();
                            break;
                        case 2:
                            int aid = Convert.ToInt32(row.Cells["ActID"].Value);
                            var a = context.Act.Find(aid);
                            if (a == null) return;
                            var newAct = AdminInputBox("Номер акта:", "Изменить", a.ActNumber);
                            if (string.IsNullOrEmpty(newAct) || newAct == a.ActNumber) return;
                            if (context.Act.Any(x => x.ActID != aid && x.ActNumber == newAct)) { MessageBox.Show("Такой акт уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            a.ActNumber = newAct;
                            context.SaveChanges();
                            break;
                        case 3:
                            int did = Convert.ToInt32(row.Cells["DescriptionID"].Value);
                            var d = context.Description.Find(did);
                            if (d == null) return;
                            var newDesc = AdminInputBox("Текст описания:", "Изменить", d.DescriptionText ?? "");
                            if (newDesc == null) return;
                            d.DescriptionText = newDesc;
                            context.SaveChanges();
                            break;
                        case 4:
                            int pid = Convert.ToInt32(row.Cells["PlaceID"].Value);
                            var pl = context.Place.Find(pid);
                            if (pl == null) return;
                            var newPlace = AdminInputBox("Название места:", "Изменить", pl.PlaceName ?? "");
                            if (string.IsNullOrEmpty(newPlace) || newPlace == pl.PlaceName) return;
                            if (context.Place.Any(x => x.PlaceID != pid && x.PlaceName == newPlace)) { MessageBox.Show("Такое место уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            pl.PlaceName = newPlace;
                            context.SaveChanges();
                            break;
                        case 5:
                            int rid = Convert.ToInt32(row.Cells["ResultID"].Value);
                            var res = context.ResultTable.Find(rid);
                            if (res == null) return;
                            var newRes = AdminInputBox("Текст результата:", "Изменить", res.ResultText ?? "");
                            if (newRes == null) return;
                            res.ResultText = newRes;
                            context.SaveChanges();
                            break;
                        case 6:
                            int roleId = Convert.ToInt32(row.Cells["RoleID"].Value);
                            var role = context.Role.Find(roleId);
                            if (role == null) return;
                            var newRole = AdminInputBox("Название роли:", "Изменить", role.RoleName ?? "");
                            if (string.IsNullOrEmpty(newRole) || newRole == role.RoleName) return;
                            if (context.Role.Any(x => x.RoleID != roleId && x.RoleName == newRole)) { MessageBox.Show("Такая роль уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            role.RoleName = newRole;
                            context.SaveChanges();
                            break;
                        case 7:
                            int permId = Convert.ToInt32(row.Cells["PermissionsID"].Value);
                            var perm = context.Permissions.Find(permId);
                            if (perm == null) return;
                            var newPerm = AdminInputBox("Название разрешения:", "Изменить", perm.PermissionsName ?? "");
                            if (string.IsNullOrEmpty(newPerm) || newPerm == perm.PermissionsName) return;
                            if (context.Permissions.Any(x => x.PermissionsID != permId && x.PermissionsName == newPerm)) { MessageBox.Show("Такое разрешение уже есть.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            perm.PermissionsName = newPerm;
                            context.SaveChanges();
                            break;
                    }
                }
                LoadAdminDataTable();
                if (_cmbAdminTable.SelectedIndex == 0 || _cmbAdminTable.SelectedIndex == 1) { LoadAdminCountries(); LoadAdminProductTypes(); }
                if (_cmbAdminTable.SelectedIndex == 2) LoadAdminActs();
                MessageBox.Show("Запись изменена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void AdminDataDelete_Click(object sender, EventArgs e)
        {
            if (_dgvAdminData?.SelectedRows.Count == 0) { MessageBox.Show("Выберите строку.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var row = _dgvAdminData.SelectedRows[0];
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    switch (_cmbAdminTable.SelectedIndex)
                    {
                        case 0:
                            var c = context.Country.Find(Convert.ToInt32(row.Cells["CountryID"].Value));
                            if (c != null) { context.Country.Remove(c); context.SaveChanges(); }
                            break;
                        case 1:
                            var t = context.ProducType.Find(Convert.ToInt32(row.Cells["TypeID"].Value));
                            if (t != null) { context.ProducType.Remove(t); context.SaveChanges(); }
                            break;
                        case 2:
                            var a = context.Act.Find(Convert.ToInt32(row.Cells["ActID"].Value));
                            if (a != null) { context.Act.Remove(a); context.SaveChanges(); }
                            break;
                        case 3:
                            var d = context.Description.Find(Convert.ToInt32(row.Cells["DescriptionID"].Value));
                            if (d != null) { context.Description.Remove(d); context.SaveChanges(); }
                            break;
                        case 4:
                            var pl = context.Place.Find(Convert.ToInt32(row.Cells["PlaceID"].Value));
                            if (pl != null) { context.Place.Remove(pl); context.SaveChanges(); }
                            break;
                        case 5:
                            var res = context.ResultTable.Find(Convert.ToInt32(row.Cells["ResultID"].Value));
                            if (res != null) { context.ResultTable.Remove(res); context.SaveChanges(); }
                            break;
                        case 6:
                            var role = context.Role.Find(Convert.ToInt32(row.Cells["RoleID"].Value));
                            if (role != null) { context.Role.Remove(role); context.SaveChanges(); }
                            break;
                        case 7:
                            var perm = context.Permissions.Find(Convert.ToInt32(row.Cells["PermissionsID"].Value));
                            if (perm != null) { context.Permissions.Remove(perm); context.SaveChanges(); }
                            break;
                    }
                }
                LoadAdminDataTable();
                if (_cmbAdminTable.SelectedIndex == 0 || _cmbAdminTable.SelectedIndex == 1) { LoadAdminCountries(); LoadAdminProductTypes(); }
                if (_cmbAdminTable.SelectedIndex == 2) LoadAdminActs();
                MessageBox.Show("Запись удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка (возможно, есть связанные записи): " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        /// <summary>Контекстное меню грида: переименование и удаление.</summary>
        private void AttachProductGridContextMenu(DataGridView dgv, Action refreshAfterChange)
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

        private void ProductGrid_Rename(DataGridView dgv, Action refreshAfterChange)
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
                newSerial = TransliterateCyrillicToLatin(newSerial).ToUpper();
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

        private void ProductGrid_Delete(DataGridView dgv, Action refreshAfterChange)
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

        #region Акты и продукты

        /// <summary>Извлечение номера акта из текста вида «Акт № 123 (5 шт.)».</summary>
        private static string ExtractActNumber(string displayText)
        {
            if (string.IsNullOrEmpty(displayText) || displayText == "(Все акты)" || displayText == "Акты не найдены") return null;
            string text = displayText.Replace("Акт № ", "");
            int parenIdx = text.IndexOf(" (");
            if (parenIdx >= 0) text = text.Substring(0, parenIdx);
            return text.Trim();
        }

        /// <summary>Элемент фильтра актов по категории (и опционально производителю).</summary>
        private class ActCategoryFilterItem
        {
            public string Display { get; set; }
            public int TypeID { get; set; }
            public int? CountryID { get; set; }
            /// <summary>Название категории при фильтре без учёта производителя.</summary>
            public string TypeName { get; set; }
        }

        private void LoadActCategories()
        {
            if (_cmbActCategory == null) return;
            try
            {
                var list = new List<ActCategoryFilterItem> { new ActCategoryFilterItem { Display = "Все категории", TypeID = 0, CountryID = null, TypeName = null } };
                using (var context = ConnectionHelper.CreateContext())
                {
                    if (_chkActByManufacturer != null && _chkActByManufacturer.Checked)
                    {
                        var withCountry = context.ProducType.AsNoTracking().Include(t => t.Country).OrderBy(t => t.TypeName).ThenBy(t => t.Country != null ? t.Country.CountryName : "").ToList();
                        foreach (var t in withCountry)
                        {
                            string disp = t.Country != null ? $"{t.TypeName} ({t.Country.CountryName})" : t.TypeName;
                            list.Add(new ActCategoryFilterItem { Display = disp, TypeID = t.TypeID, CountryID = t.CountryID, TypeName = null });
                        }
                    }
                    else
                    {
                        var distinctNames = context.ProducType.AsNoTracking().Select(t => t.TypeName).Distinct().OrderBy(n => n).ToList();
                        foreach (var name in distinctNames)
                            list.Add(new ActCategoryFilterItem { Display = name, TypeID = 0, CountryID = null, TypeName = name });
                    }
                }
                _cmbActCategory.DataSource = null;
                _cmbActCategory.DisplayMember = "Display";
                _cmbActCategory.DataSource = list;
                if (_cmbActCategory.Items.Count > 0) _cmbActCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Загрузка отгруженных актов с фильтром по категории.</summary>
        private void LoadActs()
        {
            if (lstActs == null) return;
            try
            {
                string filterTypeName = null;
                int? filterTypeID = null;
                int? filterCountryID = null;
                if (_cmbActCategory != null && _cmbActCategory.SelectedItem is ActCategoryFilterItem sel)
                {
                    if (!string.IsNullOrEmpty(sel.TypeName))
                    {
                        filterTypeName = sel.TypeName;
                    }
                    else if (sel.TypeID != 0)
                    {
                        filterTypeID = sel.TypeID;
                        filterCountryID = sel.CountryID;
                    }
                }

                using (var context = ConnectionHelper.CreateContext())
                {
                    var query = context.Act.AsNoTracking().Where(a => a.IsReady).AsQueryable();
                    if (!string.IsNullOrEmpty(filterTypeName))
                    {
                        query = query.Where(a => a.Product.Any(p => p.ProducType != null && p.ProducType.TypeName == filterTypeName));
                    }
                    else if (filterTypeID.HasValue)
                    {
                        if (filterCountryID.HasValue)
                            query = query.Where(a => a.Product.Any(p => p.TypeID == filterTypeID.Value && p.ProducType != null && p.ProducType.CountryID == filterCountryID.Value));
                        else
                            query = query.Where(a => a.Product.Any(p => p.TypeID == filterTypeID.Value));
                    }
                    var acts = query.Select(a => new { a.ActNumber, Count = a.Product.Count }).OrderBy(a => a.ActNumber).ToList();

                    lstActs.Items.Clear();
                    if (acts.Count == 0)
                    {
                        lstActs.Items.Add("Акты не найдены");
                    }
                    else
                    {
                        lstActs.Items.Add("(Все акты)");
                        foreach (var act in acts)
                            lstActs.Items.Add($"Акт № {act.ActNumber} ({act.Count} шт.)");
                    }
                    if (lstActs.Items.Count > 0)
                        lstActs.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки актов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lstActs_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebuildWorkTabsForSelectedAct();
            LoadProductsForSelectedAct();
            LoadPathForSelectedAct();
        }

        private TabPage TabPageForWorkMode(WorkMode mode)
        {
            if (mode == WorkMode.Assembly) return tabPageAssembly;
            if (mode == WorkMode.Testing) return tabPageTesting;
            if (mode == WorkMode.Inspection) return tabPageInspection;
            if (mode == WorkMode.PolletnikAutoTesting) return _tabPagePolletnikAutoTest;
            if (mode == WorkMode.CrossPlataAutoTesting) return _tabPageCrossAutoTest;
            return tabPageAssembly;
        }

        private ActProductTestingKind GetActProductTestingKind()
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedActText) || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
                return ActProductTestingKind.Standard;
            string actNumber = ExtractActNumber(selectedActText);
            if (string.IsNullOrEmpty(actNumber))
                return ActProductTestingKind.Standard;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                var act = ctx.Act.AsNoTracking()
                    .Include(a => a.Product)
                    .Include("Product.ProducType")
                    .FirstOrDefault(a => a.ActNumber == actNumber);
                if (act?.Product == null || act.Product.Count == 0)
                    return ActProductTestingKind.Standard;
                bool hasCross = act.Product.Any(p => p.ProducType != null && p.ProducType.TypeName == CrossPlateDbHelper.CrossProductTypeName);
                bool hasPol = act.Product.Any(p => p.ProducType != null && p.ProducType.TypeName == PolletnikiTypeName);
                if (hasCross)
                    return ActProductTestingKind.CrossPlata;
                if (hasPol)
                    return ActProductTestingKind.Polletniki;
            }
            return ActProductTestingKind.Standard;
        }

        /// <summary>Перестраивает вкладки режима работы: для актов только с полётниками или только с кросс-платами — отдельная вкладка авто-теста вместо «Сборка»/«Тестирование».</summary>
        private void RebuildWorkTabsForSelectedAct()
        {
            if (_baselineWorkModes.Count == 0)
                return;

            ActProductTestingKind kind = GetActProductTestingKind();
            tabControlWork.SuspendLayout();
            try
            {
                tabControlWork.TabPages.Clear();
                _visibleModes.Clear();

                bool specialized = _hasTesting && (kind == ActProductTestingKind.Polletniki || kind == ActProductTestingKind.CrossPlata);

                if (specialized && kind == ActProductTestingKind.Polletniki)
                {
                    _visibleModes.Add(WorkMode.PolletnikAutoTesting);
                    tabControlWork.TabPages.Add(_tabPagePolletnikAutoTest);
                    if (_hasInspection)
                    {
                        _visibleModes.Add(WorkMode.Inspection);
                        tabControlWork.TabPages.Add(tabPageInspection);
                    }
                }
                else if (specialized && kind == ActProductTestingKind.CrossPlata)
                {
                    _visibleModes.Add(WorkMode.CrossPlataAutoTesting);
                    tabControlWork.TabPages.Add(_tabPageCrossAutoTest);
                    if (_hasInspection)
                    {
                        _visibleModes.Add(WorkMode.Inspection);
                        tabControlWork.TabPages.Add(tabPageInspection);
                    }
                }
                else
                {
                    foreach (WorkMode m in _baselineWorkModes)
                    {
                        _visibleModes.Add(m);
                        tabControlWork.TabPages.Add(TabPageForWorkMode(m));
                    }
                }

                tabControlWork.Visible = _visibleModes.Count > 1;
                if (tabControlWork.Visible)
                    tabControlWork.SelectedIndex = 0;
                _currentWorkMode = _visibleModes[0];
            }
            finally
            {
                tabControlWork.ResumeLayout();
            }

            UpdateTesterActionButtons();
        }

        private void UpdateTesterActionButtons()
        {
            if (!_hasTesting || btnCrossPlateTesting == null || btnAdvancedTesting == null || btnBridgeTesting == null)
                return;
            if (tabControlActions == null || !tabControlActions.TabPages.Contains(tabPageActionsTester))
                return;
            ActProductTestingKind kind = GetActProductTestingKind();
            btnCrossPlateTesting.Visible = kind != ActProductTestingKind.Polletniki;
            btnAdvancedTesting.Visible = kind != ActProductTestingKind.CrossPlata;
            btnBridgeTesting.Visible = true;
        }

        /// <summary>Перезагрузка профиля и вкладок после смены роли/разрешений у текущего пользователя (без перелогина).</summary>
        private void ReloadCurrentUserFromDatabaseAndSyncShell()
        {
            if (IsDisposed) return;
            try
            {
                using (var ctx = ConnectionHelper.CreateContext())
                {
                    var fresh = ctx.UsersProfile
                        .Include(u => u.Role)
                        .Include("UserWithPermissions.Permissions")
                        .FirstOrDefault(u => u.UserID == _currentUser.UserID);
                    if (fresh == null) return;
                    _currentUser = fresh;
                }

                _isAdmin = _currentUser.Role != null && _currentUser.Role.RoleName == "Admin";
                var perms = _currentUser.UserWithPermissions ?? new List<UserWithPermissions>();
                _hasAdminPermission = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Администратор");
                _hasAssembly = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Сборщик");
                _hasTesting = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Тестировщик");
                _hasInspection = perms.Any(p => p.Permissions != null && p.Permissions.PermissionsName == "Инспектор");

                Text = "Сотрудник — " + (_currentUser.UserName ?? "");

                SyncMainAdminTabVisibility();
                SyncAdminUsersSubTabVisibility();
                SyncActionTabsToPermissions();
                RebuildBaselineWorkModesFromPermissionFlags();
                UpdateTesterActionButtons();
                RebuildWorkTabsForSelectedAct();
                btnChangeStatus.Visible = _hasAdminPermission;
                LoadProductsForSelectedAct();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось обновить интерфейс после смены прав: " + ex.Message, "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SyncMainAdminTabVisibility()
        {
            if (_hasAdminPermission)
            {
                if (!mainTabControl.TabPages.Contains(tabAdmin))
                    mainTabControl.TabPages.Insert(Math.Min(1, mainTabControl.TabPages.Count), tabAdmin);
            }
            else
            {
                if (mainTabControl.TabPages.Contains(tabAdmin))
                {
                    if (mainTabControl.SelectedTab == tabAdmin)
                        mainTabControl.SelectedIndex = 0;
                    mainTabControl.TabPages.Remove(tabAdmin);
                }
            }
        }

        private void SyncAdminUsersSubTabVisibility()
        {
            if (!_hasAdminPermission) return;
            if (_isAdmin)
            {
                if (!adminTabControl.TabPages.Contains(tabAdminUsers))
                    adminTabControl.TabPages.Insert(0, tabAdminUsers);
            }
            else
            {
                if (adminTabControl.TabPages.Contains(tabAdminUsers))
                {
                    if (adminTabControl.SelectedTab == tabAdminUsers)
                        adminTabControl.SelectedIndex = 0;
                    adminTabControl.TabPages.Remove(tabAdminUsers);
                }
            }
        }

        private void SyncActionTabsToPermissions()
        {
            if (tabControlActions.TabPages.Contains(tabPageActionsAdmin))
                tabControlActions.TabPages.Remove(tabPageActionsAdmin);
            if (tabControlActions.TabPages.Contains(tabPageActionsTester))
                tabControlActions.TabPages.Remove(tabPageActionsTester);
            int insert = 1;
            if (_hasAdminPermission)
                tabControlActions.TabPages.Insert(insert++, tabPageActionsAdmin);
            if (_hasTesting)
                tabControlActions.TabPages.Insert(insert, tabPageActionsTester);
        }

        private void RebuildBaselineWorkModesFromPermissionFlags()
        {
            _baselineWorkModes.Clear();
            if (_hasAssembly) _baselineWorkModes.Add(WorkMode.Assembly);
            if (_hasTesting) _baselineWorkModes.Add(WorkMode.Testing);
            if (_hasInspection) _baselineWorkModes.Add(WorkMode.Inspection);
            if (_baselineWorkModes.Count == 0)
                _baselineWorkModes.Add(WorkMode.Assembly);
        }

        /// <summary>Загрузка продуктов выбранного акта в грид.</summary>
        private void LoadProductsForSelectedAct()
        {
            try
            {
                string selectedActText = lstActs.SelectedItem?.ToString();
                if (selectedActText == "Акты не найдены")
                {
                    _allProductData = new List<TechnicalMapView>();
                    ApplyFilters();
                    return;
                }
                using (var context = ConnectionHelper.CreateContext())
                {
                    bool allActs = selectedActText == null || selectedActText == "(Все акты)";
                    string actNumber = allActs ? null : ExtractActNumber(selectedActText);

                    if (_currentWorkMode == WorkMode.PolletnikAutoTesting || _currentWorkMode == WorkMode.CrossPlataAutoTesting)
                    {
                        string typeName = _currentWorkMode == WorkMode.PolletnikAutoTesting
                            ? PolletnikiTypeName
                            : CrossPlateDbHelper.CrossProductTypeName;
                        var productsQuery = context.Product
                            .AsNoTracking()
                            .Include(p => p.ProducType)
                            .Include(p => p.ProducType.Country)
                            .Include(p => p.Act)
                            .Where(p => p.Act != null && p.ProducType != null && p.ProducType.TypeName == typeName);
                        if (!allActs)
                            productsQuery = productsQuery.Where(p => p.Act.ActNumber == actNumber);

                        var products = productsQuery.OrderBy(p => p.ProductSerial).ToList();
                        var productIds = products.Select(p => p.ProductID).ToList();
                        Dictionary<int, TechnicalMapFull> fullByProduct = new Dictionary<int, TechnicalMapFull>();
                        if (productIds.Count > 0)
                        {
                            var fullRows = context.TechnicalMapFull
                                .AsNoTracking()
                                .Include("TechnicalMapTesting.UsersProfile")
                                .Include("TechnicalMapTesting.Description")
                                .Where(f => productIds.Contains(f.ProductID))
                                .ToList();
                            foreach (var g in fullRows.GroupBy(f => f.ProductID))
                                fullByProduct[g.Key] = g.OrderByDescending(f => f.TMID).First();
                        }

                        _allProductData = products.Select(p =>
                        {
                            fullByProduct.TryGetValue(p.ProductID, out TechnicalMapFull f);
                            var tst = f?.TechnicalMapTesting != null && f.TechnicalMapTesting.Count > 0
                                ? f.TechnicalMapTesting.OrderByDescending(t => t.TMTID).First()
                                : null;
                            return new TechnicalMapView
                            {
                                TMID = tst != null ? tst.TMTID : 0,
                                FullTMID = f?.TMID ?? 0,
                                ProductID = p.ProductID,
                                SerialNumber = p.ProductSerial,
                                Category = p.ProducType != null ? p.ProducType.TypeName : "",
                                Act = p.Act != null ? p.Act.ActNumber : "—",
                                FullName = tst?.UsersProfile?.UserName ?? "",
                                Manufacturer = p.ProducType?.Country?.CountryName ?? "",
                                Date = tst != null ? tst.Date : DateTime.MinValue,
                                TimeStart = tst?.TimeStart ?? TimeSpan.Zero,
                                TimeEnd = tst?.TimeEnd ?? TimeSpan.Zero,
                                InProgress = tst != null && tst.InProgress,
                                IsReady = tst != null && tst.IsReadt,
                                UserID = tst?.UserID ?? 0,
                                Inspection = tst != null && tst.Fault,
                                IsTesterView = true
                            };
                        }).ToList();
                    }
                    else if (_currentWorkMode == WorkMode.Testing)
                    {
                        var queryTester = context.TechnicalMapFull
                            .AsNoTracking()
                            .Include(f => f.Product)
                            .Include(f => f.Product.ProducType)
                            .Include(f => f.Product.ProducType.Country)
                            .Include(f => f.Product.Act)
                            .Include("TechnicalMapAssembly")
                            .Include("TechnicalMapTesting.UsersProfile")
                            .Include("TechnicalMapTesting.Description")
                            .Where(f => f.Product.Act != null && !f.Inspection && f.TechnicalMapAssembly.Any(a => a.IsReady));
                        if (!allActs)
                            queryTester = queryTester.Where(f => f.Product.Act.ActNumber == actNumber);

                        _allProductData = queryTester.ToList()
                            .Select(f => {
                                var tst = f.TechnicalMapTesting.OrderByDescending(t => t.TMTID).FirstOrDefault();
                                return new TechnicalMapView
                                {
                                    TMID = tst != null ? tst.TMTID : 0,
                                    FullTMID = f.TMID,
                                    ProductID = f.ProductID,
                                    SerialNumber = f.Product.ProductSerial,
                                    Category = f.Product.ProducType != null ? f.Product.ProducType.TypeName : "",
                                    Act = f.Product.Act != null ? f.Product.Act.ActNumber : "—",
                                    FullName = tst?.UsersProfile?.UserName ?? "",
                                    Manufacturer = f.Product.ProducType?.Country?.CountryName ?? "",
                                    Date = tst != null ? tst.Date : DateTime.MinValue,
                                    TimeStart = tst?.TimeStart ?? TimeSpan.Zero,
                                    TimeEnd = tst?.TimeEnd ?? TimeSpan.Zero,
                                    InProgress = tst != null && tst.InProgress,
                                    IsReady = tst != null && tst.IsReadt,
                                    UserID = tst?.UserID ?? 0,
                                    Inspection = tst != null && tst.Fault,
                                    IsTesterView = true
                                };
                            })
                            .ToList();
                    }
                    else if (_currentWorkMode == WorkMode.Assembly)
                    {
                        var query = context.TechnicalMapAssembly
                            .AsNoTracking()
                            .Include(tm => tm.TechnicalMapFull.Product)
                            .Include(tm => tm.TechnicalMapFull.Product.ProducType)
                            .Include(tm => tm.TechnicalMapFull.Product.ProducType.Country)
                            .Include(tm => tm.TechnicalMapFull.Product.Act)
                            .Include(tm => tm.UsersProfile)
                            .AsQueryable();

                        if (!allActs)
                            query = query.Where(tm => tm.TechnicalMapFull.Product.Act != null && tm.TechnicalMapFull.Product.Act.ActNumber == actNumber);

                        _allProductData = query.ToList()
                            .Select(tm => new TechnicalMapView
                            {
                                TMID = tm.TMAID,
                                FullTMID = tm.TechnicalMapFull.TMID,
                                ProductID = tm.TechnicalMapFull.ProductID,
                                SerialNumber = tm.TechnicalMapFull.Product.ProductSerial,
                                Category = tm.TechnicalMapFull.Product.ProducType != null ? tm.TechnicalMapFull.Product.ProducType.TypeName : "",
                                Act = tm.TechnicalMapFull.Product.Act != null ? tm.TechnicalMapFull.Product.Act.ActNumber : "—",
                                FullName = tm.UsersProfile != null ? tm.UsersProfile.UserName : "",
                                Manufacturer = tm.TechnicalMapFull.Product.ProducType?.Country?.CountryName ?? "",
                                Date = tm.Date,
                                TimeStart = tm.TimeStart,
                                TimeEnd = tm.TimeEnd,
                                InProgress = tm.InProgress,
                                IsReady = tm.IsReady,
                                UserID = tm.UserID,
                                Inspection = tm.Fault,
                                IsTesterView = false
                            })
                            .ToList();

                        var productsWithActQuery = context.Product
                            .AsNoTracking()
                            .Include(p => p.ProducType)
                            .Include(p => p.ProducType.Country)
                            .Include(p => p.Act)
                            .Where(p => p.Act != null);
                        if (!allActs)
                            productsWithActQuery = productsWithActQuery.Where(p => p.Act.ActNumber == actNumber);

                        var productsWithAct = productsWithActQuery.ToList();
                        var existingProductIds = _allProductData.Select(d => d.ProductID).ToHashSet();

                        foreach (var p in productsWithAct.Where(p => !existingProductIds.Contains(p.ProductID)))
                        {
                            _allProductData.Add(new TechnicalMapView
                            {
                                TMID = 0,
                                FullTMID = 0,
                                ProductID = p.ProductID,
                                SerialNumber = p.ProductSerial,
                                Category = p.ProducType != null ? p.ProducType.TypeName : "",
                                Act = p.Act != null ? p.Act.ActNumber : "—",
                                FullName = "",
                                Manufacturer = p.ProducType?.Country?.CountryName ?? "",
                                Date = DateTime.MinValue,
                                TimeStart = TimeSpan.Zero,
                                TimeEnd = TimeSpan.Zero,
                                InProgress = false,
                                IsReady = false,
                                UserID = 0,
                                Inspection = false,
                                IsTesterView = false
                            });
                        }
                    }
                    else
                    {
                        IQueryable<Error> errorsQuery = context.Error
                            .AsNoTracking()
                            .Include("Place")
                            .Include("Product")
                            .Include("Product.ProducType")
                            .Include("Product.Act")
                            .Include("TechnicalMapFull")
                            .Include("TechnicalMapFull.Product")
                            .Include("TechnicalMapFull.Product.ProducType")
                            .Include("TechnicalMapFull.Product.Act")
                            .Include("Inspection")
                            .Include("Inspection.UsersProfile")
                            .Include("Inspection.ResultTable");

                        if (!allActs)
                        {
                            errorsQuery = errorsQuery.Where(er =>
                                (er.TechnicalMapFull != null && er.TechnicalMapFull.Product.Act != null && er.TechnicalMapFull.Product.Act.ActNumber == actNumber)
                                || (er.Product != null && er.Product.Act != null && er.Product.Act.ActNumber == actNumber));
                        }

                        _allProductData = errorsQuery.ToList().Select(er =>
                        {
                            var prod = er.TechnicalMapFull != null ? er.TechnicalMapFull.Product : er.Product;
                            var firstInspection = er.Inspection != null && er.Inspection.Count > 0 ? er.Inspection.First() : null;
                            bool hasInspection = firstInspection != null;
                            return new TechnicalMapView
                            {
                                TMID = er.ErrorID,
                                ProductID = prod != null ? prod.ProductID : 0,
                                SerialNumber = prod?.ProductSerial ?? "",
                                Category = prod?.ProducType?.TypeName ?? "",
                                Act = prod?.Act?.ActNumber ?? "—",
                                FullName = er.Place?.PlaceName ?? "",
                                Manufacturer = prod?.ProducType?.Country?.CountryName ?? "",
                                Date = er.Date,
                                TimeStart = TimeSpan.Zero,
                                TimeEnd = TimeSpan.Zero,
                                InProgress = er.inProgress,
                                IsReady = hasInspection,
                                UserID = 0,
                                Inspection = true,
                                IsTesterView = false,
                                InspectorName = hasInspection ? (firstInspection.UsersProfile?.UserName ?? "") : "",
                                InspectionDateStr = er.Date.ToString("dd.MM.yyyy"),
                                ResultText = hasInspection ? (firstInspection.ResultTable?.ResultText ?? "") : ""
                            };
                        }).ToList();
                    }

                    if (_currentWorkMode == WorkMode.Assembly || _currentWorkMode == WorkMode.Testing
                        || _currentWorkMode == WorkMode.PolletnikAutoTesting || _currentWorkMode == WorkMode.CrossPlataAutoTesting)
                    {
                        var errorsWithInsp = context.Error
                            .AsNoTracking()
                            .Where(e => e.TMID != null)
                            .Include("Inspection")
                            .Include("Inspection.ResultTable")
                            .ToList();
                        var tmidApproved = errorsWithInsp
                            .Where(e => e.Inspection != null && e.Inspection.Any(i => i.ResultTable != null &&
                                i.ResultTable.ResultText != null &&
                                i.ResultTable.ResultText.IndexOf("Отклонение разрешено", StringComparison.OrdinalIgnoreCase) >= 0))
                            .Select(e => e.TMID.Value)
                            .Distinct()
                            .ToHashSet();
                        foreach (var d in _allProductData)
                        {
                            if (d.FullTMID != 0 && tmidApproved.Contains(d.FullTMID))
                                d.AfterInspection = true;
                        }
                    }

                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Применение фильтров к данным грида и подсветка строк по статусу.</summary>
        private void ApplyFilters()
        {
            var filtered = _allProductData.AsEnumerable();

            string searchText = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(d =>
                    (d.Act != null && d.Act.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (d.SerialNumber != null && d.SerialNumber.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (chkDateFilter.Checked)
            {
                DateTime dateFrom = dtpDateFrom.Value.Date;
                DateTime dateTo = dtpDateTo.Value.Date;
                filtered = filtered.Where(d => d.Date != DateTime.MinValue &&
                    d.Date.Date >= dateFrom && d.Date.Date <= dateTo);
            }

            if (chkTimeFilter.Checked)
            {
                TimeSpan timeFrom = dtpTimeFrom.Value.TimeOfDay;
                TimeSpan timeTo = dtpTimeTo.Value.TimeOfDay;
                filtered = filtered.Where(d =>
                    d.TimeStart >= timeFrom && d.TimeStart <= timeTo);
            }

            bool isTesterView = _currentWorkMode == WorkMode.Testing
                || _currentWorkMode == WorkMode.PolletnikAutoTesting
                || _currentWorkMode == WorkMode.CrossPlataAutoTesting;
            bool isInspectionMode = _currentWorkMode == WorkMode.Inspection;

            dgvProducts.SuspendLayout();

            object displayData;
            if (isInspectionMode)
            {
                displayData = filtered
                    .Select(d => new
                    {
                        d.TMID,
                        d.ProductID,
                        Inspector = d.InspectorName ?? "",
                        ActSerial = (d.Act ?? "—") + " / " + (d.SerialNumber ?? ""),
                        RequestDate = d.InspectionDateStr ?? "",
                        Status = d.IsReady ? "Готово" : (d.InProgress ? "В работе" : "Свободно"),
                        Result = d.ResultText ?? "",
                        InProgress = d.InProgress,
                        IsReady = d.IsReady,
                        Inspection = true,
                        IsTesterView = false
                    })
                    .ToList();

                dgvProducts.DataSource = displayData;

                dgvProducts.Columns["TMID"].Visible = false;
                dgvProducts.Columns["ProductID"].Visible = false;
                dgvProducts.Columns["InProgress"].Visible = false;
                dgvProducts.Columns["IsReady"].Visible = false;
                dgvProducts.Columns["Inspection"].Visible = false;
                dgvProducts.Columns["IsTesterView"].Visible = false;

                dgvProducts.Columns["Inspector"].HeaderText = "Инспектор";
                dgvProducts.Columns["ActSerial"].HeaderText = "Акт / серийный номер";
                dgvProducts.Columns["RequestDate"].HeaderText = "Дата обращения";
                dgvProducts.Columns["Status"].HeaderText = "Статус";
                dgvProducts.Columns["Result"].HeaderText = "Результат";

                int order = 0;
                dgvProducts.Columns["Inspector"].DisplayIndex = order++;
                dgvProducts.Columns["ActSerial"].DisplayIndex = order++;
                dgvProducts.Columns["RequestDate"].DisplayIndex = order++;
                dgvProducts.Columns["Status"].DisplayIndex = order++;
                dgvProducts.Columns["Result"].DisplayIndex = order++;
            }
            else
            {
                displayData = filtered
                    .Select(d => new
                    {
                        d.TMID,
                        d.ProductID,
                        d.IsTesterView,
                        Assembler = d.FullName,
                        Manufacturer = d.Manufacturer ?? "",
                        SerialNumber = d.SerialNumber,
                        AssemblyDate = d.Date == DateTime.MinValue ? "" : d.Date.ToString("dd.MM.yyyy"),
                        AssemblyTime = (d.TimeStart == TimeSpan.Zero && d.TMID == 0) ? "" : (d.TimeStart.ToString(@"hh\:mm") + "-" + d.TimeEnd.ToString(@"hh\:mm")),
                        Status = d.AfterInspection ? "После инспекции" : (d.Inspection ? "Инспекция" : (d.IsReady ? (isTesterView ? "Завершено" : "Передан на тестирование") : (d.InProgress ? "В работе" : "Свободно"))),
                        InProgress = d.InProgress,
                        IsReady = d.IsReady,
                        Inspection = d.Inspection
                    })
                    .ToList();

                dgvProducts.DataSource = displayData;

                if (dgvProducts.Columns.Contains("TMID"))
                    dgvProducts.Columns["TMID"].Visible = false;
                if (dgvProducts.Columns.Contains("ProductID"))
                    dgvProducts.Columns["ProductID"].Visible = false;
                if (dgvProducts.Columns.Contains("IsTesterView"))
                    dgvProducts.Columns["IsTesterView"].Visible = false;
                if (dgvProducts.Columns.Contains("InProgress"))
                    dgvProducts.Columns["InProgress"].Visible = false;
                if (dgvProducts.Columns.Contains("IsReady"))
                    dgvProducts.Columns["IsReady"].Visible = false;
                if (dgvProducts.Columns.Contains("Inspection"))
                    dgvProducts.Columns["Inspection"].Visible = false;

                if (dgvProducts.Columns.Contains("Assembler"))
                    dgvProducts.Columns["Assembler"].HeaderText = isTesterView ? "Тестировщик" : "Сборщик";
                if (dgvProducts.Columns.Contains("Manufacturer"))
                    dgvProducts.Columns["Manufacturer"].HeaderText = "Изготовитель";
                if (dgvProducts.Columns.Contains("SerialNumber"))
                    dgvProducts.Columns["SerialNumber"].HeaderText = "Серийный номер";
                if (dgvProducts.Columns.Contains("AssemblyDate"))
                    dgvProducts.Columns["AssemblyDate"].HeaderText = isTesterView ? "Дата тестирования" : "Дата сборки";
                if (dgvProducts.Columns.Contains("AssemblyTime"))
                    dgvProducts.Columns["AssemblyTime"].HeaderText = isTesterView ? "Время начала и окончания тестирования" : "Время начала и окончания сборки";
                if (dgvProducts.Columns.Contains("Status"))
                    dgvProducts.Columns["Status"].HeaderText = "Статус";
                int idx = 0;
                if (dgvProducts.Columns.Contains("Assembler")) dgvProducts.Columns["Assembler"].DisplayIndex = idx++;
                if (dgvProducts.Columns.Contains("Manufacturer")) dgvProducts.Columns["Manufacturer"].DisplayIndex = idx++;
                if (dgvProducts.Columns.Contains("SerialNumber")) dgvProducts.Columns["SerialNumber"].DisplayIndex = idx++;
                if (dgvProducts.Columns.Contains("AssemblyDate")) dgvProducts.Columns["AssemblyDate"].DisplayIndex = idx++;
                if (dgvProducts.Columns.Contains("AssemblyTime")) dgvProducts.Columns["AssemblyTime"].DisplayIndex = idx++;
                if (dgvProducts.Columns.Contains("Status")) dgvProducts.Columns["Status"].DisplayIndex = idx++;
            }

            foreach (DataGridViewRow row in dgvProducts.Rows)
            {
                if (!dgvProducts.Columns.Contains("InProgress") || !dgvProducts.Columns.Contains("IsReady") || !dgvProducts.Columns.Contains("Inspection"))
                    continue;
                bool inProgress = (bool)row.Cells["InProgress"].Value;
                bool ready = (bool)row.Cells["IsReady"].Value;
                bool inspection = (bool)row.Cells["Inspection"].Value;

                if (inspection)
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                else if (ready)
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                else if (inProgress)
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightYellow;
            }

            dgvProducts.ResumeLayout();
        }

        /// <summary>Двойной клик: бронь/открытие формы работы с продуктом; при OBS — запись и перенос видео.</summary>
        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvProducts.Rows[e.RowIndex];
            int tmId = (int)row.Cells["TMID"].Value;
            int productId = (int)row.Cells["ProductID"].Value;
            bool inProgress = (bool)row.Cells["InProgress"].Value;
            bool ready = (bool)row.Cells["IsReady"].Value;

            if (_currentWorkMode == WorkMode.PolletnikAutoTesting)
            {
                TryOpenPolletnikAutoTestingForSelectedAct();
                return;
            }
            if (_currentWorkMode == WorkMode.CrossPlataAutoTesting)
            {
                TryOpenCrossPlateAutoTestingForSelectedAct();
                return;
            }

            if (_currentWorkMode == WorkMode.Testing)
            {
                if (!ObsConfig.IsConfigured())
                {
                    MessageBox.Show("Для тестирования продуктов необходимо настроить подключение к OBS.\n" +
                        "Откройте «Настройки OBS» на странице авторизации.",
                        "OBS не настроен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (_currentWorkMode == WorkMode.Inspection)
            {
                int errorId = tmId;
                if (errorId == 0)
                {
                    MessageBox.Show("Нет ярлыка несоответствия для этого продукта.", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (ready)
                {
                    MessageBox.Show("Инспекция уже проведена.", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var context = ConnectionHelper.CreateContext())
                {
                    var err = context.Error.Find(errorId);
                    if (err != null)
                    {
                        err.inProgress = true;
                        context.SaveChanges();
                    }
                }

                using (var inspForm = new InspectionWorkForm(errorId, _currentUser))
                {
                    inspForm.ShowDialog(this);
                }
                LoadProductsForSelectedAct();
                return;
            }

            if (ready)
            {
                MessageBox.Show("Этот продукт уже проверен и завершён.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (inProgress)
            {
                var existingItem = _allProductData.FirstOrDefault(d => d.ProductID == productId && d.TMID == tmId);
                if (existingItem != null && existingItem.UserID != _currentUser.UserID)
                {
                    MessageBox.Show("Этот продукт уже забронирован другим сотрудником.", "Внимание",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                int openedTmId = 0;
                var dataItem = _allProductData.FirstOrDefault(d => d.ProductID == productId && d.TMID == tmId);
                string serial = (row.Cells["SerialNumber"].Value ?? "").ToString();
                string category = dataItem?.Category ?? "";
                string fio = (row.Cells["Assembler"].Value ?? "").ToString();
                string dateStr = (row.Cells["AssemblyDate"].Value ?? "").ToString();
                DateTime date = DateTime.Now;
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var parsedDate))
                    date = parsedDate;
                TimeSpan timeStart = dataItem?.TimeStart ?? TimeSpan.Zero;
                TimeSpan timeEnd = dataItem?.TimeEnd ?? TimeSpan.Zero;

                using (var context = ConnectionHelper.CreateContext())
                {
                    bool openAsTesting = (_currentWorkMode == WorkMode.Testing);

                    if (tmId == 0)
                    {
                        var full = context.TechnicalMapFull.FirstOrDefault(f => f.ProductID == productId);
                        if (full == null)
                        {
                            full = new TechnicalMapFull { ProductID = productId, Inspection = false };
                            context.TechnicalMapFull.Add(full);
                            context.SaveChanges();
                        }

                        if (openAsTesting)
                        {
                             var tst = new TechnicalMapTesting
                             {
                                 TMID = full.TMID,
                                 UserID = _currentUser.UserID,
                                 Date = DateTime.Now,
                                 TimeStart = TimeSpan.Zero,
                                 TimeEnd = TimeSpan.Zero,
                                 InProgress = true,
                                 IsReadt = false,
                                 Fault = false
                             };
                             context.TechnicalMapTesting.Add(tst);
                             context.SaveChanges();
                             openedTmId = tst.TMTID;
                             date = tst.Date;
                        }
                        else
                        {
                            var asm = new TechnicalMapAssembly
                            {
                                TMID = full.TMID,
                                UserID = _currentUser.UserID,
                                Date = DateTime.Now,
                                TimeStart = TimeSpan.Zero,
                                TimeEnd = TimeSpan.Zero,
                                InProgress = true,
                                IsReady = false,
                                Fault = false
                            };
                            context.TechnicalMapAssembly.Add(asm);
                            context.SaveChanges();
                            openedTmId = asm.TMAID;
                            date = asm.Date;
                        }
                        fio = _currentUser.UserName ?? fio;
                    }
                    else
                    {
                        if (openAsTesting)
                        {
                            var tm = context.TechnicalMapTesting.Find(tmId);
                            if (tm != null && tm.UserID == _currentUser.UserID && tm.InProgress)
                            {
                                openedTmId = tmId;
                                date = tm.Date;
                                timeStart = tm.TimeStart;
                                timeEnd = tm.TimeEnd;
                                if (string.IsNullOrEmpty(fio))
                                    fio = _currentUser.UserName ?? "";
                            }
                            else if (tm != null && !tm.InProgress && !tm.Fault && !tm.IsReadt)
                            {
                                tm.InProgress = true;
                                tm.UserID = _currentUser.UserID;
                                context.SaveChanges();
                                openedTmId = tmId;
                                date = tm.Date;
                                timeStart = tm.TimeStart;
                                timeEnd = tm.TimeEnd;
                                fio = _currentUser.UserName ?? "";
                            }
                            else if (tm != null && tm.UserID == _currentUser.UserID && !tm.InProgress)
                            {
                                MessageBox.Show("Продукт уже завершён.", "Информация",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }
                        else
                        {
                            var tm = context.TechnicalMapAssembly.Find(tmId);
                            if (tm != null && tm.UserID == _currentUser.UserID && tm.InProgress)
                            {
                                openedTmId = tmId;
                                date = tm.Date;
                                timeStart = tm.TimeStart;
                                timeEnd = tm.TimeEnd;
                                if (string.IsNullOrEmpty(fio))
                                    fio = _currentUser.UserName ?? "";
                            }
                            else if (tm != null && !tm.InProgress && !tm.Fault && !tm.IsReady)
                            {
                                tm.InProgress = true;
                                tm.UserID = _currentUser.UserID;
                                context.SaveChanges();
                                openedTmId = tmId;
                                date = tm.Date;
                                timeStart = tm.TimeStart;
                                timeEnd = tm.TimeEnd;
                                fio = _currentUser.UserName ?? "";
                            }
                            else if (tm != null && tm.UserID == _currentUser.UserID && !tm.InProgress)
                            {
                                MessageBox.Show("Продукт уже завершён.", "Информация",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                }

                if (openedTmId != 0)
                {
                    bool passIsTesting = (_currentWorkMode == WorkMode.Testing);

                    if (passIsTesting)
                    {
                        var obsResult = MessageBox.Show(
                            "После нажатия кнопки «ОК» автоматически будет включена запись.",
                            "Запись тестирования", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                        if (obsResult != DialogResult.OK)
                        {
                            LoadProductsForSelectedAct();
                            return;
                        }

                        ObsConfig.Load(out string obsIp, out int obsPort, out string obsPwd);
                        using (var obs = new ObsWebSocketHelper())
                        {
                            bool obsConnected = obs.Connect(obsIp, obsPort, obsPwd);
                            if (!obsConnected)
                            {
                                MessageBox.Show("Не удалось подключиться к OBS.\n" +
                                    "Убедитесь, что OBS запущен и WebSocket-сервер включён.\n" +
                                    "Тестирование без записи невозможно.",
                                    "Ошибка OBS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                LoadProductsForSelectedAct();
                                return;
                            }

                            bool recordStarted = obs.StartRecording();
                            if (!recordStarted)
                            {
                                MessageBox.Show("Не удалось начать запись в OBS.",
                                    "Ошибка OBS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                LoadProductsForSelectedAct();
                                return;
                            }

                            DialogResult formResult;
                            using (var form = new ProductWorkForm(openedTmId, serial, category, fio, date, timeStart, timeEnd, true))
                            {
                                formResult = form.ShowDialog(this);
                            }

                            string obsOutputPath = obs.StopRecording();

                            Thread.Sleep(1500);

                            if (formResult == DialogResult.OK)
                            {
                                bool videoOk = string.IsNullOrEmpty(obsOutputPath) ||
                                    MoveTestVideoToProductFolder(obsOutputPath, serial, productId);
                                if (videoOk)
                                    MessageBox.Show("Успешно сохранено.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else
                    {
                        using (var form = new ProductWorkForm(openedTmId, serial, category, fio, date, timeStart, timeEnd, false))
                        {
                            if (form.ShowDialog(this) == DialogResult.OK)
                                MessageBox.Show("Успешно сохранено.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }

                LoadProductsForSelectedAct();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка бронирования: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Перемещение записанного OBS-видео в папку продукта (test_{serial}.расширение).</summary>
        private bool MoveTestVideoToProductFolder(string obsFilePath, string serial, int productId)
        {
            try
            {
                if (!File.Exists(obsFilePath))
                {
                    MessageBox.Show("Файл записи OBS не найден:\n" + obsFilePath,
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                string actNumber = null;
                using (var context = ConnectionHelper.CreateContext())
                {
                    var product = context.Product.Include(p => p.Act).FirstOrDefault(p => p.ProductID == productId);
                    if (product?.Act != null)
                        actNumber = product.Act.ActNumber;
                }

                if (string.IsNullOrEmpty(actNumber))
                {
                    MessageBox.Show("Продукт не привязан к акту — невозможно определить папку для сохранения видео.",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                string actFolder;
                using (var context = ConnectionHelper.CreateContext())
                {
                    actFolder = context.GetSavePathForAct(actNumber);
                }

                if (string.IsNullOrEmpty(actFolder))
                {
                    MessageBox.Show("Путь папки акта не задан в настройках. Сначала создайте папки акта.",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (!Directory.Exists(actFolder))
                    Directory.CreateDirectory(actFolder);

                string productFolder = Path.Combine(actFolder, serial);
                if (!Directory.Exists(productFolder))
                    Directory.CreateDirectory(productFolder);

                string ext = Path.GetExtension(obsFilePath);
                string destFile = Path.Combine(productFolder, "test_" + serial + ext);

                if (File.Exists(destFile))
                    File.Delete(destFile);

                const int maxRetries = 6;
                const int delayMs = 800;
                bool moved = false;
                for (int attempt = 0; attempt < maxRetries && !moved; attempt++)
                {
                    if (attempt > 0)
                        Thread.Sleep(delayMs);
                    try
                    {
                        File.Move(obsFilePath, destFile);
                        moved = true;
                    }
                    catch (IOException)
                    {
                        if (attempt == maxRetries - 1)
                            throw;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения видео тестирования:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            LoadProductsForSelectedAct();
            MessageBox.Show("Список обновлён.", "Обновление", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Создание ярлыка несоответствия для выбранного продукта (только статус «Свободно»).</summary>
        private void btnNonConformity_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукт в таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgvProducts.SelectedRows[0];
            string status = row.Cells["Status"].Value?.ToString()?.Trim() ?? "";
            bool allowed = (status == "Свободно");
            if (!allowed)
            {
                MessageBox.Show("Ярлык несоответствия можно создавать только для продуктов со статусом «Свободно».", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int productId = (int)row.Cells["ProductID"].Value;
            var dataItem = _allProductData.FirstOrDefault(d => d.ProductID == productId);
            string serial = dataItem?.SerialNumber ?? "";
            string category = dataItem?.Category ?? "";
            string actNumber = dataItem?.Act ?? "";

            // PlaceID = 1 (место по справочнику для ярлыка до работы / у выбранного продукта)
            using (var form = new NonConformityForm(productId, serial, category, actNumber,
                _currentUser.UserName ?? "", false, 1))
            {
                form.ShowDialog(this);
            }
            LoadProductsForSelectedAct();
        }

        /// <summary>Смена статуса продукта («В работе» / «Готово») через диалог.</summary>
        private void btnChangeStatus_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите строку с продуктом в таблице.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var row = dgvProducts.SelectedRows[0];
            int tmId = Convert.ToInt32(row.Cells["TMID"].Value);
            if (tmId == 0)
            {
                MessageBox.Show("Для продукта без записи о работе статус меняется при первом открытии формы.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool inProgress = (bool)row.Cells["InProgress"].Value;
            bool ready = (bool)row.Cells["IsReady"].Value;

            using (var form = new ChangeStatusForm(tmId, inProgress, ready))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    LoadProductsForSelectedAct();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadActs();
            LoadProductsForSelectedAct();
        }

        /// <summary>Создание папок акта (базовый путь + подпапки по серийным номерам).</summary>
        private void btnCreateActFolders_Click(object sender, EventArgs e)
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
            {
                MessageBox.Show("Выберите конкретный акт из списка (не «Все акты»).", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string actNumber = ExtractActNumber(selectedActText);

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите базовый путь для папок акта";
                if (!string.IsNullOrEmpty(txtUserPath.Text) && Directory.Exists(txtUserPath.Text))
                {
                    string parent = Path.GetDirectoryName(txtUserPath.Text);
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                        fbd.SelectedPath = parent;
                }
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;

                string basePath = fbd.SelectedPath;
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    MessageBox.Show("Путь не выбран.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    string categoryName = "Без категории";
                    List<Product> productsInAct;
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        productsInAct = context.Product
                            .Include(p => p.ProducType)
                            .Where(p => p.Act != null && p.Act.ActNumber == actNumber)
                            .ToList();
                        var firstProduct = productsInAct.FirstOrDefault();
                        if (firstProduct?.ProducType != null && !string.IsNullOrEmpty(firstProduct.ProducType.TypeName))
                            categoryName = firstProduct.ProducType.TypeName;
                    }

                    if (productsInAct.Count == 0)
                    {
                        MessageBox.Show("В выбранном акте нет продуктов.", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string actFolderName = "Отгрузка_" + categoryName + "_Акт_" + actNumber;
                    string actFolder = Path.Combine(basePath, actFolderName);
                    Directory.CreateDirectory(actFolder);

                    foreach (var product in productsInAct)
                    {
                        if (!string.IsNullOrEmpty(product.ProductSerial))
                        {
                            string productFolder = Path.Combine(actFolder, product.ProductSerial);
                            Directory.CreateDirectory(productFolder);
                        }
                    }

                    using (var context = ConnectionHelper.CreateContext())
                    {
                        context.SetSavePathForAct(actNumber, actFolder);
                    }

                    _pathOverrides[actNumber] = actFolder;
                    _isLoadingPath = true;
                    txtUserPath.Text = actFolder;
                    _isLoadingPath = false;

                    MessageBox.Show("Папки акта созданы:\n" + actFolder, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка создания папок: " + ex.Message, "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnAdvancedTesting_Click(object sender, EventArgs e) => TryOpenPolletnikAutoTestingForSelectedAct();

        /// <summary>Обновление dgvProducts с потока формы сотрудника; вызывается из формы авто-теста при сохранении в БД.</summary>
        private void RefreshEmployeeProductGridFromTesting()
        {
            if (IsDisposed) return;
            void reload() { LoadProductsForSelectedAct(); }
            if (InvokeRequired)
                BeginInvoke(new Action(reload));
            else
                reload();
        }

        /// <summary>Открывает форму авто-теста полётников для текущего выбранного акта.</summary>
        private bool TryOpenPolletnikAutoTestingForSelectedAct()
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
            {
                MessageBox.Show("Выберите конкретный акт из списка для тестирования полетников.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string actNumber = ExtractActNumber(selectedActText);
            if (string.IsNullOrEmpty(actNumber))
            {
                MessageBox.Show("Не удалось определить номер акта.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int? actId = null;
            bool hasPoletniki = false;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                var act = ctx.Act.AsNoTracking().Include(a => a.Product).Include("Product.ProducType")
                    .FirstOrDefault(a => a.ActNumber == actNumber);
                if (act == null)
                {
                    MessageBox.Show("Акт не найден в базе данных.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                actId = act.ActID;
                hasPoletniki = act.Product.Any(p => p.ProducType != null && p.ProducType.TypeName == PolletnikiTypeName);
            }

            if (!hasPoletniki)
            {
                MessageBox.Show("В выбранном акте нет продуктов типа «Полетники».", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            using (var form = new AutoTestingForm(_currentUser, actNumber, actId.Value, actHasCrossPlata: false, crossOnlyMode: false, RefreshEmployeeProductGridFromTesting))
            {
                form.ShowDialog(this);
            }
            LoadProductsForSelectedAct();
            return true;
        }

        private void btnCrossPlateTesting_Click(object sender, EventArgs e) => TryOpenCrossPlateAutoTestingForSelectedAct();

        private void btnBridgeTesting_Click(object sender, EventArgs e) => TryOpenBridgeLogForSelectedAct();

        /// <summary>Форма сохранения логов ESP32 Bridge для текущего акта и пользователя.</summary>
        private bool TryOpenBridgeLogForSelectedAct()
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
            {
                MessageBox.Show("Выберите конкретный акт из списка.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string actNumber = ExtractActNumber(selectedActText);
            if (string.IsNullOrEmpty(actNumber))
            {
                MessageBox.Show("Не удалось определить номер акта.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int actId;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                var act = ctx.Act.AsNoTracking().FirstOrDefault(a => a.ActNumber == actNumber);
                if (act == null)
                {
                    MessageBox.Show("Акт не найден в базе данных.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                actId = act.ActID;
            }

            using (var form = new BridgeLogForm(_currentUser, actId, actNumber))
            {
                form.ShowDialog(this);
            }
            return true;
        }

        /// <summary>Открывает форму авто-теста кросс-плат для текущего выбранного акта.</summary>
        private bool TryOpenCrossPlateAutoTestingForSelectedAct()
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
            {
                MessageBox.Show("Выберите конкретный акт из списка для тестирования кросс-плат.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string actNumber = ExtractActNumber(selectedActText);
            if (string.IsNullOrEmpty(actNumber))
            {
                MessageBox.Show("Не удалось определить номер акта.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int? actId = null;
            bool hasCrossPlata = false;
            using (var ctx = ConnectionHelper.CreateContext())
            {
                var act = ctx.Act.AsNoTracking().Include(a => a.Product).Include("Product.ProducType")
                    .FirstOrDefault(a => a.ActNumber == actNumber);
                if (act == null)
                {
                    MessageBox.Show("Акт не найден в базе данных.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                actId = act.ActID;
                hasCrossPlata = act.Product.Any(p => p.ProducType != null && p.ProducType.TypeName == CrossPlateDbHelper.CrossProductTypeName);
            }

            if (!hasCrossPlata)
            {
                MessageBox.Show("В выбранном акте нет продуктов типа «Кросс-плата».", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            using (var form = new AutoTestingForm(_currentUser, actNumber, actId.Value, true, crossOnlyMode: true, RefreshEmployeeProductGridFromTesting))
            {
                form.ShowDialog(this);
            }
            LoadProductsForSelectedAct();
            return true;
        }

        private void btnGenerateQrEmployee_Click(object sender, EventArgs e)
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
            {
                MessageBox.Show("Выберите конкретный акт из списка для генерации QR-кодов", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string actNumber = ExtractActNumber(selectedActText);
            GenerateQrCodesForActByNumber(actNumber);
        }

        private void GenerateQrCodesForActByNumber(string actNumber)
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template.docx");

            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Файл шаблона 'Template.docx' не найден в папке с программой.\n\nПожалуйста, выберите ваш файл-шаблон (Пр Godex ОБРАЗЕЦ.docx). Он будет скопирован в папку с программой для дальнейшего автоматического использования.", "Шаблон не найден", MessageBoxButtons.OK, MessageBoxIcon.Information);
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Выберите файл-шаблон Word (Пр Godex ОБРАЗЕЦ.docx)";
                    ofd.Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*";
                    if (ofd.ShowDialog() != DialogResult.OK)
                        return;

                    try
                    {
                        File.Copy(ofd.FileName, templatePath, true);
                        MessageBox.Show("Шаблон успешно сохранен! Теперь он будет использоваться автоматически.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось скопировать шаблон: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            string savePath = "";
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Сохранить итоговый документ с QR-кодами как...";
                sfd.Filter = "Word Documents (*.docx)|*.docx";
                sfd.FileName = $"Акт_№{actNumber}_QR.docx";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;
                savePath = sfd.FileName;
            }

            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document finalDoc = null;

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

                    wordApp = new Microsoft.Office.Interop.Word.Application();
                    wordApp.Visible = false;
                    wordApp.Options.SmartCutPaste = false;

                    finalDoc = wordApp.Documents.Add(templatePath);
                    finalDoc.Content.Copy();

                    int generated = 0;
                    for (int i = 0; i < products.Count; i++)
                    {
                        var product = products[i];
                        string serial = product.ProductSerial;
                        if (string.IsNullOrEmpty(serial))
                            continue;

                        string tempImg = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
                        QrCodeHelper.SaveQrCode(serial, tempImg);

                        Microsoft.Office.Interop.Word.Range workRange;

                        if (i == 0)
                        {
                            workRange = finalDoc.Content;
                        }
                        else
                        {
                            var pasteRange = finalDoc.Content;
                            pasteRange.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);

                            pasteRange.Text = "\r";
                            pasteRange.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);

                            int start = pasteRange.Start;
                            pasteRange.Paste();
                            int end = finalDoc.Content.End;
                            workRange = finalDoc.Range(start, end);
                            RemoveLeadingParagraphFromPreviousLineIfAtPageStart(finalDoc, workRange);
                            RemoveLeadingEmptyParagraph(workRange);
                        }

                        var findObj = workRange.Find;
                        findObj.ClearFormatting();
                        findObj.Replacement.ClearFormatting();
                        findObj.Execute("993", ReplaceWith: actNumber, Replace: Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll);

                        findObj.Execute("Серийный", ReplaceWith: serial, Replace: Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll);

                        var findQr = workRange.Find;
                        findQr.ClearFormatting();
                        findQr.Text = "QR код";
                        if (findQr.Execute())
                        {
                            Microsoft.Office.Interop.Word.Range rangeQr = findQr.Parent;
                            rangeQr.Text = "";
                            object linkToFile = false;
                            object saveWithDoc = true;
                            rangeQr.InlineShapes.AddPicture(tempImg, ref linkToFile, ref saveWithDoc);
                        }

                        if (File.Exists(tempImg))
                            File.Delete(tempImg);

                        generated++;
                    }

                    try
                    {
                        var endRange = finalDoc.Content;
                        endRange.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
                        if (endRange.Start > 0)
                        {
                            endRange.MoveStart(Microsoft.Office.Interop.Word.WdUnits.wdCharacter, -1);
                            string lastCh = endRange.Text ?? "";
                            if (lastCh.Length == 1 && (lastCh[0] == '\r' || lastCh[0] == '\n' || (int)lastCh[0] == 13 || (int)lastCh[0] == 10))
                                endRange.Text = "";
                        }
                    }
                    catch { /* игнорируем ошибки при удалении последнего символа */ }

                    finalDoc.SaveAs2(savePath);

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
                if (finalDoc != null)
                {
                    finalDoc.Close(Microsoft.Office.Interop.Word.WdSaveOptions.wdDoNotSaveChanges);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(finalDoc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
            }
        }

        /// <summary>Удаление ведущих символов абзаца в начале диапазона.</summary>
        private static void RemoveLeadingEmptyParagraph(Microsoft.Office.Interop.Word.Range workRange)
        {
            if (workRange == null || workRange.Start >= workRange.End) return;
            const int maxRemove = 20;
            for (int i = 0; i < maxRemove; i++)
            {
                if (workRange.Start >= workRange.End) return;
                var first = workRange.Duplicate;
                first.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseStart);
                first.MoveEnd(Microsoft.Office.Interop.Word.WdUnits.wdCharacter, 1);
                string t = first.Text ?? "";
                if (t.Length != 1) return;
                char c = t[0];
                if (c != '\r' && c != '\n' && (int)c != 13 && (int)c != 10) return;
                first.Text = "";
            }
        }

        /// <summary>Удаление лишнего конца абзаца после QR при переносе на следующую страницу.</summary>
        private static void RemoveLeadingParagraphFromPreviousLineIfAtPageStart(Microsoft.Office.Interop.Word.Document doc, Microsoft.Office.Interop.Word.Range workRange)
        {
            if (doc == null || workRange == null || workRange.Start <= 1 || workRange.Start >= workRange.End) return;
            try
            {
                var firstChar = workRange.Duplicate;
                firstChar.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseStart);
                firstChar.MoveEnd(Microsoft.Office.Interop.Word.WdUnits.wdCharacter, 1);
                string firstText = firstChar.Text ?? "";
                if (firstText.Length != 1) return;
                char c = firstText[0];
                if (c != '\r' && c != '\n' && (int)c != 13 && (int)c != 10) return;

                var beforeRange = doc.Range(workRange.Start - 1, workRange.Start);
                string t = beforeRange.Text ?? "";
                if (t.Length != 1 || (t[0] != '\r' && t[0] != '\n' && (int)t[0] != 13 && (int)t[0] != 10)) return;
                int pageAtStart = (int)workRange.Information[Microsoft.Office.Interop.Word.WdInformation.wdActiveEndPageNumber];
                var prevRange = doc.Range(workRange.Start - 1, workRange.Start - 1);
                int pageBefore = (int)prevRange.Information[Microsoft.Office.Interop.Word.WdInformation.wdActiveEndPageNumber];
                if (pageAtStart > pageBefore) return;
                beforeRange.Text = "";
            }
            catch { /* игнорируем ошибки */ }
        }

        #endregion

        #region Фильтры

        private void FilterChanged(object sender, EventArgs e)
        {
            if (sender == txtSearch)
            {
                ApplyFilters();
            }
        }

        /// <summary>Замена кириллицы на латиницу при выходе из поля поиска.</summary>
        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
            txtSearch.Text = TransliterateOnlyCyrillic(txtSearch.Text);
        }

        /// <summary>Транслитерация кириллицы в латиницу с сохранением регистра.</summary>
        private static string TransliterateOnlyCyrillic(string text)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (IsCyrillic(c))
                {
                    string lat = TransliterateCyrillicToLatin(c.ToString());
                    if (char.IsUpper(c)) lat = lat.ToUpper();
                    sb.Append(lat);
                }
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Мгновенная транслитерация кириллицы в латиницу при вводе.</summary>
        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!IsCyrillic(e.KeyChar)) return;
            e.Handled = true;
            var tb = (TextBox)sender;
            string converted = TransliterateCyrillicToLatin(e.KeyChar.ToString());
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

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void btnResetFilter_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            chkDateFilter.Checked = false;
            chkTimeFilter.Checked = false;
            dtpDateFrom.Value = DateTime.Today;
            dtpDateTo.Value = DateTime.Today;
            ApplyFilters();
        }

        #endregion

        #region Экспорт в Excel

        /// <summary>Экспорт в Excel: выбор актов/даты/времени, два листа — «Отчёт» и «В работе».</summary>
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                using (var optionsForm = new ExportExcelOptionsForm())
                {
                    if (optionsForm.ShowDialog(this) != DialogResult.OK) return;

                    List<IGrouping<string, TechnicalMapFull>> data;
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var query = context.TechnicalMapFull
                            .AsNoTracking()
                            .Include(f => f.Product)
                            .Include(f => f.Product.Act)
                            .Include(f => f.Product.ProducType)
                            .Include(f => f.Product.ProducType.Country)
                            .Include("TechnicalMapAssembly.UsersProfile")
                            .Include("TechnicalMapAssembly.Description")
                            .Include("TechnicalMapTesting.UsersProfile")
                            .Include("TechnicalMapTesting.Description")
                            .AsQueryable();

                        switch (optionsForm.Mode)
                        {
                            case ExportExcelMode.SelectedActs:
                                var actNumbers = optionsForm.SelectedActNumbers;
                                query = query.Where(f => f.Product.Act != null && actNumbers.Contains(f.Product.Act.ActNumber));
                                break;
                            case ExportExcelMode.ByDate:
                                var dateFrom = optionsForm.DateFrom;
                                var dateTo = optionsForm.DateTo;
                                query = query.Where(f => f.TechnicalMapAssembly.Any(a => a.Date >= dateFrom && a.Date <= dateTo) || f.TechnicalMapTesting.Any(t => t.Date >= dateFrom && t.Date <= dateTo));
                                break;
                            case ExportExcelMode.ByTime:
                                var timeFrom = optionsForm.TimeFrom;
                                var timeTo = optionsForm.TimeTo;
                                query = query.Where(f => f.TechnicalMapAssembly.Any(a => a.TimeStart >= timeFrom && a.TimeStart <= timeTo) || f.TechnicalMapTesting.Any(t => t.TimeStart >= timeFrom && t.TimeStart <= timeTo));
                                break;
                        }

                        data = query
                            .ToList()
                            .Where(f => f.TechnicalMapAssembly.Any() || f.TechnicalMapTesting.Any())
                            .GroupBy(f => f.Product.Act != null ? f.Product.Act.ActNumber : "Без акта")
                            .OrderBy(g => g.Key)
                            .ToList();
                    }

                    if (data.Count == 0)
                    {
                        MessageBox.Show("Нет данных для экспорта по выбранным условиям.", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var faultFulls = data.SelectMany(g => g).Where(f => f.Inspection || f.TechnicalMapAssembly.Any(a => a.Fault) || f.TechnicalMapTesting.Any(t => t.Fault)).ToList();
                    Dictionary<int, Tuple<string, string, string, int>> inspectionByTmId = new Dictionary<int, Tuple<string, string, string, int>>();
                    if (faultFulls.Any())
                    {
                        using (var ctx = ConnectionHelper.CreateContext())
                        {
                            var fullIds = faultFulls.Select(f => f.TMID).Distinct().ToList();
                            var inspections = ctx.Inspection
                                .AsNoTracking()
                                .Include(i => i.Error)
                                .Include(i => i.UsersProfile)
                                .Include(i => i.ResultTable)
                                .Include(i => i.Description)
                                .Where(i => i.Error.TMID.HasValue && fullIds.Contains(i.Error.TMID.Value))
                                .ToList();
                            foreach (var f in faultFulls)
                            {
                                var insp = inspections.FirstOrDefault(i => i.Error.TMID == f.TMID);
                                if (insp == null) continue;
                                int errorId = insp.ErrorID;
                                var commentText = FaultDescriptionHelper.GetInspectionCommentTexts(errorId);
                                string comment = !string.IsNullOrEmpty(commentText)
                                    ? commentText
                                    : (insp.Description != null ? insp.Description.DescriptionText : "");
                                inspectionByTmId[f.TMID] = Tuple.Create(insp.UsersProfile?.UserName ?? "", comment, insp.ResultTable?.ResultText ?? "", insp.DescriptionID);
                            }
                        }
                    }

                    var allTmIds = data.SelectMany(g => g).Select(f => f.TMID).Distinct().ToList();
                    Dictionary<int, Tuple<string, string, int>> afterInspectionByTmId = new Dictionary<int, Tuple<string, string, int>>();
                    if (allTmIds.Any())
                    {
                        using (var ctx = ConnectionHelper.CreateContext())
                        {
                            var errorsWithInsp = ctx.Error
                                .AsNoTracking()
                                .Where(err => err.TMID != null && allTmIds.Contains(err.TMID.Value))
                                .Include("Inspection")
                                .Include("Inspection.UsersProfile")
                                .Include("Inspection.ResultTable")
                                .Include("Inspection.Description")
                                .ToList();
                            foreach (var err in errorsWithInsp)
                            {
                                var firstApproved = err.Inspection?.FirstOrDefault(i => i.ResultTable != null && i.ResultTable.ResultText != null &&
                                    i.ResultTable.ResultText.IndexOf("Отклонение разрешено", StringComparison.OrdinalIgnoreCase) >= 0);
                                if (firstApproved == null || !err.TMID.HasValue) continue;
                                string faultText = string.Join(", ", FaultDescriptionHelper.GetErrorDefectTexts(err.ErrorID, err.TMID.Value) ?? new List<string>());
                                if (string.IsNullOrWhiteSpace(faultText)) faultText = firstApproved.Description?.DescriptionText ?? "";
                                string inspectorName = firstApproved.UsersProfile?.UserName ?? "";
                                afterInspectionByTmId[err.TMID.Value] = Tuple.Create(faultText, inspectorName, err.PlaceID);
                            }
                        }
                    }

                    string targetFile = null;
                    using (var ctx = ConnectionHelper.CreateContext())
                    {
                        targetFile = ctx.GetSavePathForAct("Excel");
                    }
                    if (string.IsNullOrEmpty(targetFile) || !System.IO.File.Exists(targetFile))
                    {
                        SaveFileDialog sfd = new SaveFileDialog();
                        sfd.Filter = "Excel файлы (*.xlsx)|*.xlsx";
                        sfd.FileName = "Отчёт_" + DateTime.Now.ToString("dd.MM.yyyy");
                        if (sfd.ShowDialog() != DialogResult.OK) return;
                        targetFile = sfd.FileName;
                        using (var ctx = ConnectionHelper.CreateContext())
                        {
                            ctx.SetSavePathForAct("Excel", targetFile);
                        }
                    }

                    Excel.Application excelApp = null;
                    Excel.Workbook workbook = null;
                    Excel.Worksheet wsMain = null;
                    Excel.Worksheet wsWork = null;

                    try
                    {
                        excelApp = new Excel.Application();
                        excelApp.Visible = false;
                        workbook = excelApp.Workbooks.Add();
                        wsMain = (Excel.Worksheet)workbook.Sheets[1];
                        wsMain.Name = "Отчёт";
                        wsWork = (Excel.Worksheet)workbook.Sheets.Add(After: wsMain);
                        wsWork.Name = "В работе";

                        string[] headersMain = { "№", "Сборщик", "Изготовитель", "S/N платы", "Дата сборки в корпус", "Время видеозаписи и сборки", "Передан на тест", "Коммент", "Тестировщик", "Дата тестирования", "Время видеозаписи и тест", "Допуск", "Коммент" };
                        string[] headersWork = { "№", "Сборщик", "Изготовитель", "S/N платы", "Дата сборки в корпус", "Время видеозаписи и сборки", "Передан на тест", "Коммент", "Тестировщик", "Дата тестирования", "Время видеозаписи и тест", "Допуск", "Коммент", "Инспекция", "Коммент", "Результат", "Подтверждено" };

                        int rowMain = 1, rowWork = 1;
                        WriteSheetHeaders(wsMain, rowMain, headersMain); rowMain++;
                        WriteSheetHeaders(wsWork, rowWork, headersWork); rowWork++;

                        foreach (var actGroup in data)
                        {
                            var okList = actGroup.Where(f => !f.Inspection && !(f.TechnicalMapAssembly.Any(a => a.Fault) || f.TechnicalMapTesting.Any(t => t.Fault))).OrderBy(p => p.Product.ProductSerial).ToList();
                            var faultList = actGroup.Where(f => f.Inspection || f.TechnicalMapAssembly.Any(a => a.Fault) || f.TechnicalMapTesting.Any(t => t.Fault)).OrderBy(p => p.Product.ProductSerial).ToList();

                            if (okList.Any())
                            {
                                WriteActHeader(wsMain, ref rowMain, actGroup.Key, headersMain.Length);
                                int num = 1;
                                foreach (var f in okList)
                                {
                                    WriteMainRow(wsMain, rowMain, num++, f, headersMain.Length, afterInspectionByTmId); rowMain++;
                                }
                                rowMain++;
                            }

                            if (faultList.Any())
                            {
                                WriteActHeader(wsWork, ref rowWork, actGroup.Key, headersWork.Length, isWork: true);
                                int num = 1;
                                foreach (var f in faultList)
                                {
                                    WriteWorkRow(wsWork, rowWork, num++, f, actGroup.Key, headersWork.Length, inspectionByTmId); rowWork++;
                                }
                                rowWork++;
                            }
                        }

                        wsMain.Columns.AutoFit();
                        wsWork.Columns.AutoFit();
                        workbook.SaveAs(targetFile);

                        MessageBox.Show("Файл успешно сохранён:\n" + targetFile, "Экспорт",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    finally
                    {
                        if (workbook != null) { workbook.Close(false); Marshal.ReleaseComObject(workbook); }
                        if (excelApp != null) { excelApp.Quit(); Marshal.ReleaseComObject(excelApp); }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта в Excel: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Запись заголовка блока акта в лист Excel.</summary>
        private void WriteActHeader(Excel.Worksheet ws, ref int row, string actKey, int colCount, bool isWork = false)
        {
            Excel.Range r = ws.Range[ws.Cells[row, 1], ws.Cells[row, colCount]];
            r.Merge();
            r.Value2 = "Акт № " + actKey;
            r.Interior.Color = System.Drawing.ColorTranslator.ToOle(isWork ? System.Drawing.Color.LightCoral : System.Drawing.Color.LightGreen);
            r.Font.Bold = true;
            r.Font.Size = 12;
            r.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            row++;
        }

        /// <summary>Запись строки заголовков в лист Excel.</summary>
        private void WriteSheetHeaders(Excel.Worksheet ws, int row, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
                ws.Cells[row, i + 1] = headers[i];
            Excel.Range hr = ws.Range[ws.Cells[row, 1], ws.Cells[row, headers.Length]];
            hr.Font.Bold = true;
            hr.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.CornflowerBlue);
            hr.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        /// <summary>Запись строки в лист «Отчёт» (продукты без брака).</summary>
        private void WriteMainRow(Excel.Worksheet ws, int row, int num, TechnicalMapFull f, int colCount, Dictionary<int, Tuple<string, string, int>> afterInspectionByTmId = null)
        {
            var asm = f.TechnicalMapAssembly.OrderByDescending(a => a.TMAID).FirstOrDefault();
            var tst = f.TechnicalMapTesting.OrderByDescending(t => t.TMTID).FirstOrDefault();
            ws.Cells[row, 1] = num;
            ws.Cells[row, 2] = asm?.UsersProfile?.UserName ?? "";
            ws.Cells[row, 3] = f.Product.ProducType?.Country?.CountryName ?? "Россия";
            ws.Cells[row, 4] = f.Product.ProductSerial;
            ws.Cells[row, 5] = asm != null ? asm.Date.ToString("dd.MM.yyyy") : "";
            ws.Cells[row, 6] = asm != null ? asm.TimeStart.ToString(@"h\:mm") + "-" + asm.TimeEnd.ToString(@"h\:mm") : "";
            ws.Cells[row, 7] = asm != null && asm.IsReady ? "+" : "";
            Tuple<string, string, int> afterInsp = null;
            if (afterInspectionByTmId != null)
                afterInspectionByTmId.TryGetValue(f.TMID, out afterInsp);
            bool isAfterInspection = afterInsp != null;
            var asmComment = (asm != null && asm.Fault) ? FaultDescriptionHelper.GetAssemblyFaultTexts(asm.TMAID) : "";
            var tstComment = (tst != null && tst.Fault) ? FaultDescriptionHelper.GetTestingFaultTexts(tst.TMTID) : "";
            if (isAfterInspection && afterInsp.Item3 == 2)
            {
                string comment8 = (afterInsp.Item1 ?? "") + "\nИнспектор: " + (afterInsp.Item2 ?? "") + "\nРезультат: Отклонение разрешено";
                ws.Cells[row, 8] = comment8;
            }
            else
            {
                ws.Cells[row, 8] = !string.IsNullOrEmpty(asmComment) ? asmComment : (asm != null && asm.Fault && asm.Description != null ? asm.Description.DescriptionText : "-");
            }
            ws.Cells[row, 9] = tst?.UsersProfile?.UserName ?? "";
            ws.Cells[row, 10] = tst != null ? tst.Date.ToString("dd.MM.yyyy") : "";
            ws.Cells[row, 11] = tst != null ? tst.TimeStart.ToString(@"h\:mm") + "-" + tst.TimeEnd.ToString(@"h\:mm") : "";
            ws.Cells[row, 12] = tst != null && tst.IsReadt ? "T" : "";
            if (isAfterInspection && afterInsp.Item3 == 3)
            {
                string comment13 = (afterInsp.Item1 ?? "") + "\nИнспектор: " + (afterInsp.Item2 ?? "") + "\nРезультат: Отклонение разрешено";
                ws.Cells[row, 13] = comment13;
            }
            else
            {
                ws.Cells[row, 13] = !string.IsNullOrEmpty(tstComment) ? tstComment : (tst != null && tst.Fault && tst.Description != null ? tst.Description.DescriptionText : "-");
            }
            Excel.Range dr = ws.Range[ws.Cells[row, 1], ws.Cells[row, colCount]];
            dr.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        /// <summary>Запись строки в лист «В работе» (брак/инспекция, подтверждено/не подтверждено).</summary>
        private void WriteWorkRow(Excel.Worksheet ws, int row, int num, TechnicalMapFull f, string actNumber, int colCount, Dictionary<int, Tuple<string, string, string, int>> inspectionByTmId)
        {
            var asm = f.TechnicalMapAssembly.OrderByDescending(a => a.TMAID).FirstOrDefault();
            var tst = f.TechnicalMapTesting.OrderByDescending(t => t.TMTID).FirstOrDefault();
            Tuple<string, string, string, int> insp = null;
            if (inspectionByTmId != null)
                inspectionByTmId.TryGetValue(f.TMID, out insp);

            ws.Cells[row, 1] = num;
            ws.Cells[row, 2] = asm?.UsersProfile?.UserName ?? "";
            ws.Cells[row, 3] = f.Product.ProducType?.Country?.CountryName ?? "Россия";
            ws.Cells[row, 4] = actNumber + "/" + f.Product.ProductSerial;
            ws.Cells[row, 5] = asm != null ? asm.Date.ToString("dd.MM.yyyy") : "";
            ws.Cells[row, 6] = asm != null ? asm.TimeStart.ToString(@"h\:mm") + "-" + asm.TimeEnd.ToString(@"h\:mm") : "";
            ws.Cells[row, 7] = asm != null && asm.IsReady ? "+" : "";
            var asmCommentW = (asm != null && asm.Fault) ? FaultDescriptionHelper.GetAssemblyFaultTexts(asm.TMAID) : "";
            var tstCommentW = (tst != null && tst.Fault) ? FaultDescriptionHelper.GetTestingFaultTexts(tst.TMTID) : "";
            ws.Cells[row, 8] = !string.IsNullOrEmpty(asmCommentW) ? asmCommentW : (asm != null && asm.Fault && asm.Description != null ? asm.Description.DescriptionText : "-");
            ws.Cells[row, 9] = tst?.UsersProfile?.UserName ?? "";
            ws.Cells[row, 10] = tst != null ? tst.Date.ToString("dd.MM.yyyy") : "";
            ws.Cells[row, 11] = tst != null ? tst.TimeStart.ToString(@"h\:mm") + "-" + tst.TimeEnd.ToString(@"h\:mm") : "";
            ws.Cells[row, 12] = tst != null && tst.IsReadt ? "T" : "";
            ws.Cells[row, 13] = !string.IsNullOrEmpty(tstCommentW) ? tstCommentW : (tst != null && tst.Fault && tst.Description != null ? tst.Description.DescriptionText : "-");
            ws.Cells[row, 14] = insp?.Item1 ?? "";
            ws.Cells[row, 15] = insp?.Item2 ?? "";
            ws.Cells[row, 16] = insp?.Item3 ?? "";
            int? asmDescId = asm != null && asm.Fault ? asm.DescriptionID : null;
            int? tstDescId = tst != null && tst.Fault ? tst.DescriptionID : null;
            string confirmed = "";
            if (insp != null)
            {
                int inspDescId = insp.Item4;
                bool match = (asmDescId.HasValue && asmDescId.Value == inspDescId) || (tstDescId.HasValue && tstDescId.Value == inspDescId);
                confirmed = match ? "Подтверждено" : "Не подтверждено";
            }
            ws.Cells[row, 17] = confirmed;
            Excel.Range dr = ws.Range[ws.Cells[row, 1], ws.Cells[row, colCount]];
            dr.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        #endregion

        #region Администрирование — пользователи

        /// <summary>Загрузка списка пользователей в грид.</summary>
        private void LoadUsers()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var users = context.UsersProfile
                        .Include(u => u.Role)
                        .ToList()
                        .Select(u => new
                        {
                            u.UserID,
                            FullName = u.UserName,
                            Login = u.UserLogin,
                            Role = u.Role != null ? u.Role.RoleName : "",
                            u.RoleID
                        })
                        .ToList();

                    dgvUsers.DataSource = users;
                    if (dgvUsers.Columns.Contains("UserID"))
                        dgvUsers.Columns["UserID"].Visible = false;
                    if (dgvUsers.Columns.Contains("RoleID"))
                        dgvUsers.Columns["RoleID"].Visible = false;
                    if (dgvUsers.Columns.Contains("FullName")) dgvUsers.Columns["FullName"].HeaderText = "ФИО";
                    if (dgvUsers.Columns.Contains("Login")) dgvUsers.Columns["Login"].HeaderText = "Логин";
                    if (dgvUsers.Columns.Contains("Role")) dgvUsers.Columns["Role"].HeaderText = "Роль";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Загрузка продуктов без акта для вкладки администрирования.</summary>
        private void LoadNoActProducts()
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

                    dgvNoActProducts.DataSource = products;
                    if (dgvNoActProducts.Columns.Contains("ProductID"))
                        dgvNoActProducts.Columns["ProductID"].Visible = false;
                    if (dgvNoActProducts.Columns.Contains("SerialNumber")) dgvNoActProducts.Columns["SerialNumber"].HeaderText = "Серийный номер";
                    if (dgvNoActProducts.Columns.Contains("Category")) dgvNoActProducts.Columns["Category"].HeaderText = "Категория";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAdminCountries()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var countries = context.Country.OrderBy(c => c.CountryName).Select(c => c.CountryName).ToList();
                    cmbAdminCountry.Items.Clear();
                    foreach (var name in countries)
                        cmbAdminCountry.Items.Add(name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки стран: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAdminProductTypes()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var types = context.ProducType.Include("Country").ToList();
                    var display = types.Select(t => new
                    {
                        t.TypeID,
                        DisplayName = t.Country != null ? $"{t.TypeName} ({t.Country.CountryName})" : t.TypeName
                    }).ToList();
                    cmbAdminProductType.DataSource = display;
                    cmbAdminProductType.DisplayMember = "DisplayName";
                    cmbAdminProductType.ValueMember = "TypeID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Транслитерация серийного номера: кириллица → латиница.</summary>
        private static string TransliterateCyrillicToLatin(string text)
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

        private void txtAdminSerial_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAdminSerial.Text)) return;
            txtAdminSerial.Text = TransliterateCyrillicToLatin(txtAdminSerial.Text).ToUpper();
        }

        private void btnAdminAddCategory_Click(object sender, EventArgs e)
        {
            string name = txtAdminNewCategory.Text.Trim();
            string countryName = (cmbAdminCountry.SelectedItem != null)
                ? cmbAdminCountry.SelectedItem.ToString()
                : cmbAdminCountry.Text.Trim();
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
                txtAdminNewCategory.Clear();
                LoadAdminProductTypes();
                LoadNoActProducts();
                MessageBox.Show("Категория добавлена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления категории: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdminAddProduct_Click(object sender, EventArgs e)
        {
            string serial = txtAdminSerial.Text.Trim();
            if (string.IsNullOrEmpty(serial))
            {
                MessageBox.Show("Введите серийный номер", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            serial = TransliterateCyrillicToLatin(serial).ToUpper();
            if (cmbAdminProductType.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию продукта", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int typeId = (int)cmbAdminProductType.SelectedValue;
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    if (context.Product.Any(p => p.ProductSerial == serial))
                    {
                        MessageBox.Show("Продукт с таким серийным номером уже существует", "Внимание",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    context.Product.Add(new Product { ProductSerial = serial, TypeID = typeId });
                    context.SaveChanges();
                }
                txtAdminSerial.Clear();
                LoadNoActProducts();
                LoadAdminUnassignedProducts();
               
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления продукта: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            using (var dialog = new UserEditDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var context = ConnectionHelper.CreateContext())
                        {
                            var user = new UsersProfile
                            {
                                UserName = dialog.UserFullName,
                                UserLogin = dialog.UserLoginValue,
                                UserPassword = PasswordHasher.HashPassword(dialog.UserPasswordValue),
                                RoleID = dialog.UserRoleID
                            };
                            context.UsersProfile.Add(user);
                            context.SaveChanges();

                            if (dialog.SelectedPermissionIds != null && dialog.SelectedPermissionIds.Count > 0)
                            {
                                foreach (var permId in dialog.SelectedPermissionIds)
                                {
                                    context.UserWithPermissions.Add(new UserWithPermissions
                                    {
                                        UserID = user.UserID,
                                        PermissionsID = permId
                                    });
                                }
                                context.SaveChanges();
                            }
                        }
                        LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка добавления: " + ex.Message, "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnEditUser_Click(object sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0) return;
            int userId = (int)dgvUsers.SelectedRows[0].Cells["UserID"].Value;

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var user = context.UsersProfile
                        .Include("Role")
                        .Include("UserWithPermissions")
                        .FirstOrDefault(u => u.UserID == userId);
                    if (user == null) return;

                    using (var dialog = new UserEditDialog(user))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            user.UserName = dialog.UserFullName;
                            user.UserLogin = dialog.UserLoginValue;
                            if (dialog.UserPasswordValue != null)
                                user.UserPassword = PasswordHasher.HashPassword(dialog.UserPasswordValue);
                            user.RoleID = dialog.UserRoleID;

                            var toRemove = context.UserWithPermissions.Where(uwp => uwp.UserID == user.UserID).ToList();
                            foreach (var uwp in toRemove)
                                context.UserWithPermissions.Remove(uwp);

                            if (dialog.SelectedPermissionIds != null)
                            {
                                foreach (var permId in dialog.SelectedPermissionIds)
                                {
                                    context.UserWithPermissions.Add(new UserWithPermissions
                                    {
                                        UserID = user.UserID,
                                        PermissionsID = permId
                                    });
                                }
                            }
                            context.SaveChanges();
                            LoadUsers();
                            if (userId == _currentUser.UserID)
                                ReloadCurrentUserFromDatabaseAndSyncShell();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeleteUser_Click(object sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0) return;
            int userId = (int)dgvUsers.SelectedRows[0].Cells["UserID"].Value;

            if (userId == _currentUser.UserID)
            {
                MessageBox.Show("Нельзя удалить самого себя.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить этого пользователя?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var user = context.UsersProfile.Find(userId);
                    if (user != null)
                    {
                        context.UsersProfile.Remove(user);
                        context.SaveChanges();
                    }
                }
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Путь пользователя (переопределение)

        private void LoadPathForSelectedAct()
        {
            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены")
            {
                txtUserPath.Text = "";
                return;
            }

            string actNumber = ExtractActNumber(selectedActText);

            if (_pathOverrides.ContainsKey(actNumber))
            {
                _isLoadingPath = true;
                txtUserPath.Text = _pathOverrides[actNumber];
                _isLoadingPath = false;
                return;
            }

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    string savedPath = context.GetSavePathForAct(actNumber);
                    _isLoadingPath = true;
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        txtUserPath.Text = Path.Combine(savedPath, actNumber);
                    }
                    else
                    {
                        txtUserPath.Text = "";
                    }
                    _isLoadingPath = false;
                }
            }
            catch
            {
                _isLoadingPath = true;
                txtUserPath.Text = "";
                _isLoadingPath = false;
            }
        }

        private void txtUserPath_TextChanged(object sender, EventArgs e)
        {
            if (_isLoadingPath) return;

            string selectedActText = lstActs.SelectedItem?.ToString();
            if (selectedActText == null || selectedActText == "(Все акты)" || selectedActText == "Акты не найдены") return;

            string actNumber = ExtractActNumber(selectedActText);
            _pathOverrides[actNumber] = txtUserPath.Text;
        }

        private void btnBrowseUserPath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите путь для акта";
                if (!string.IsNullOrEmpty(txtUserPath.Text) && Directory.Exists(txtUserPath.Text))
                {
                    fbd.SelectedPath = txtUserPath.Text;
                }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtUserPath.Text = fbd.SelectedPath;
                }
            }
        }

        #endregion

        #region Администрирование — акты и папки

        /// <summary>Загрузка списка актов на вкладке «Акты и папки».</summary>
        private void LoadAdminActs()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var acts = context.Act.ToList();
                    cmbAdminActs.DataSource = acts;
                    cmbAdminActs.DisplayMember = "ActNumber";
                    cmbAdminActs.ValueMember = "ActID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки актов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Загрузка продуктов без акта для привязки к выбранному акту.</summary>
        private void LoadAdminUnassignedProducts()
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

                    dgvAdminUnassigned.DataSource = products;
                    if (dgvAdminUnassigned.Columns.Contains("ProductID"))
                        dgvAdminUnassigned.Columns["ProductID"].Visible = false;
                    if (dgvAdminUnassigned.Columns.Contains("SerialNumber")) dgvAdminUnassigned.Columns["SerialNumber"].HeaderText = "Серийный номер";
                    if (dgvAdminUnassigned.Columns.Contains("Category")) dgvAdminUnassigned.Columns["Category"].HeaderText = "Категория";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки продуктов: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBrowsePath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Выберите базовый путь для папок акта";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtActPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnAdminCreateAct_Click(object sender, EventArgs e)
        {
            string actNumber = txtAdminActNumber.Text.Trim();

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

                    context.Act.Add(new Act { ActNumber = actNumber, IsReady = true });
                    context.SaveChanges();
                }

                txtAdminActNumber.Clear();
                LoadAdminActs();
                LoadNoActProducts();
                LoadAdminUnassignedProducts();
                MessageBox.Show("Акт создан.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка создания акта: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbAdminActs_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedAct = cmbAdminActs.SelectedItem as Act;
            if (selectedAct == null)
            {
                txtActPath.Clear();
                return;
            }
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    string savedPath = context.GetSavePathForAct(selectedAct.ActNumber);
                    txtActPath.Text = savedPath ?? "";
                }
            }
            catch
            {
                txtActPath.Clear();
            }
        }

        private void btnAdminAssign_Click(object sender, EventArgs e)
        {
            if (cmbAdminActs.SelectedItem == null)
            {
                MessageBox.Show("Выберите акт", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvAdminUnassigned.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите продукты для привязки", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedAct = cmbAdminActs.SelectedItem as Act;
            if (selectedAct == null) return;
            string actNumber = selectedAct.ActNumber;
            var selectedProductIds = dgvAdminUnassigned.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(r => (int)r.Cells["ProductID"].Value)
                .ToList();

            string basePath = txtActPath.Text.Trim();

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var act = context.Act.FirstOrDefault<Act>(a => a.ActNumber == actNumber);
                    if (act == null) { MessageBox.Show("Акт не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

                    var products = context.Product
                        .Where(p => selectedProductIds.Contains(p.ProductID))
                        .ToList();

                    foreach (var product in products)
                    {
                        product.Act = act;
                        var full = context.TechnicalMapFull.FirstOrDefault(f => f.ProductID == product.ProductID);
                        if (full == null)
                        {
                            context.TechnicalMapFull.Add(new TechnicalMapFull { ProductID = product.ProductID, Inspection = false });
                        }
                    }
                    act.IsReady = true;
                    context.SaveChanges();
                }

                if (!string.IsNullOrEmpty(basePath))
                {
                    string categoryName = "Без категории";
                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var firstProduct = context.Product
                            .Include(p => p.ProducType)
                            .FirstOrDefault(p => selectedProductIds.Contains(p.ProductID));
                        if (firstProduct?.ProducType != null && !string.IsNullOrEmpty(firstProduct.ProducType.TypeName))
                            categoryName = firstProduct.ProducType.TypeName;
                    }

                    string actFolderName = "Отгрузка_" + categoryName + "_Акт_" + actNumber;
                    string actFolder = Path.Combine(basePath, actFolderName);
                    Directory.CreateDirectory(actFolder);

                    using (var context = ConnectionHelper.CreateContext())
                    {
                        var products = context.Product
                            .Where(p => selectedProductIds.Contains(p.ProductID))
                            .ToList();
                        foreach (var product in products)
                        {
                            if (!string.IsNullOrEmpty(product.ProductSerial))
                            {
                                string productFolder = Path.Combine(actFolder, product.ProductSerial);
                                Directory.CreateDirectory(productFolder);
                            }
                        }
                        context.SetSavePathForAct(actNumber, actFolder);
                    }
                }

                LoadAdminUnassignedProducts();
                LoadNoActProducts();
                LoadActs();
                cmbAdminActs_SelectedIndexChanged(null, null);

                MessageBox.Show(
                    string.IsNullOrEmpty(basePath)
                        ? "Продукты привязаны к акту."
                        : "Продукты привязаны к акту, папки созданы.",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка привязки: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdminGenerateQr_Click(object sender, EventArgs e)
        {
            if (cmbAdminActs.SelectedItem == null)
            {
                MessageBox.Show("Выберите акт для генерации QR-кодов", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedAct = cmbAdminActs.SelectedItem as Act;
            if (selectedAct == null) return;

            GenerateQrCodesForActByNumber(selectedAct.ActNumber);
        }

        #endregion

        #region Статистика по браку

        /// <summary>Загрузка статистики по этапам и по типам брака.</summary>
        private void LoadDefectStatistics()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var errorsByPlace = context.Error
                        .Include("Place")
                        .ToList()
                        .GroupBy(e => new { e.PlaceID, PlaceName = e.Place?.PlaceName ?? "" })
                        .Select(g => new { g.Key.PlaceID, g.Key.PlaceName, Count = g.Count() })
                        .ToList();

                    int totalErrors = errorsByPlace.Sum(x => x.Count);
                    var stageData = errorsByPlace
                        .OrderByDescending(x => x.Count)
                        .Select(x => new
                        {
                            Stage = string.IsNullOrEmpty(x.PlaceName) ? "—" : x.PlaceName,
                            Count = x.Count,
                            Percent = totalErrors > 0 ? Math.Round(100.0 * x.Count / totalErrors, 2) : 0
                        })
                        .ToList();

                    dgvStatsByStage.DataSource = stageData;
                    if (dgvStatsByStage.Columns.Contains("Stage")) dgvStatsByStage.Columns["Stage"].HeaderText = "Этап";
                    if (dgvStatsByStage.Columns.Contains("Count")) dgvStatsByStage.Columns["Count"].HeaderText = "Количество";
                    if (dgvStatsByStage.Columns.Contains("Percent")) dgvStatsByStage.Columns["Percent"].HeaderText = "% от всего";

                    var assemblyByDesc = context.TechnicalMapAssembly
                        .Where(a => a.Fault && a.DescriptionID != null)
                        .GroupBy(a => a.DescriptionID)
                        .Select(g => new { DescriptionID = g.Key.Value, Count = g.Count() })
                        .ToList();

                    var testingByDesc = context.TechnicalMapTesting
                        .Where(t => t.Fault && t.DescriptionID != null)
                        .GroupBy(t => t.DescriptionID)
                        .Select(g => new { DescriptionID = g.Key.Value, Count = g.Count() })
                        .ToList();

                    var inspectionByDesc = context.Inspection
                        .GroupBy(i => i.DescriptionID)
                        .Select(g => new { DescriptionID = g.Key, Count = g.Count() })
                        .ToList();

                    var allDescIds = assemblyByDesc.Select(x => x.DescriptionID)
                        .Union(testingByDesc.Select(x => x.DescriptionID))
                        .Union(inspectionByDesc.Select(x => x.DescriptionID))
                        .Distinct()
                        .ToList();

                    var descriptions = context.Description
                        .Where(d => allDescIds.Contains(d.DescriptionID))
                        .ToDictionary(d => d.DescriptionID, d => d.DescriptionText ?? "");

                    var defectCounts = new List<(string name, int count)>();
                    foreach (int descId in allDescIds)
                    {
                        int asm = assemblyByDesc.FirstOrDefault(x => x.DescriptionID == descId)?.Count ?? 0;
                        int tst = testingByDesc.FirstOrDefault(x => x.DescriptionID == descId)?.Count ?? 0;
                        int insp = inspectionByDesc.FirstOrDefault(x => x.DescriptionID == descId)?.Count ?? 0;
                        int sum = asm + tst + insp;
                        string name = descriptions.ContainsKey(descId) ? descriptions[descId] : "—";
                        defectCounts.Add((name, sum));
                    }

                    int totalDefects = defectCounts.Sum(x => x.count);
                    var defectData = defectCounts
                        .OrderByDescending(x => x.count)
                        .Select(x => new
                        {
                            DefectType = x.name,
                            Count = x.count,
                            Percent = totalDefects > 0 ? Math.Round(100.0 * x.count / totalDefects, 2) : 0.0
                        })
                        .ToList();

                    dgvStatsByDefect.DataSource = defectData;
                    if (dgvStatsByDefect.Columns.Contains("DefectType")) dgvStatsByDefect.Columns["DefectType"].HeaderText = "Тип брака";
                    if (dgvStatsByDefect.Columns.Contains("Count")) dgvStatsByDefect.Columns["Count"].HeaderText = "Количество";
                    if (dgvStatsByDefect.Columns.Contains("Percent")) dgvStatsByDefect.Columns["Percent"].HeaderText = "% от всего";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки статистики: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefreshStats_Click(object sender, EventArgs e)
        {
            LoadDefectStatistics();
        }

        #endregion
    }
}
