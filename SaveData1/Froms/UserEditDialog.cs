using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;

namespace SaveData1
{
    public partial class UserEditDialog : Form
    {
        public string UserFullName { get; private set; }
        public string UserLoginValue { get; private set; }
        /// <summary>Пароль в открытом виде; при редактировании null — не менять пароль в БД.</summary>
        public string UserPasswordValue { get; private set; }
        public int UserRoleID { get; private set; }
        public List<int> SelectedPermissionIds { get; private set; }

        private readonly bool _editingExistingUser;

        public UserEditDialog()
        {
            InitializeComponent();
            _editingExistingUser = false;
            txtPassword.UseSystemPasswordChar = true;
            this.Text = "Добавить пользователя";
            LoadRoles();
            LoadPermissions();
        }

        public UserEditDialog(UsersProfile user) : this()
        {
            _editingExistingUser = true;
            this.Text = "Изменить пользователя";
            txtName.Text = user.UserName;
            txtLogin.Text = user.UserLogin;
            txtPassword.Clear();
            lblPassword.Text = "Пароль (пусто — не менять):";

            for (int i = 0; i < cmbRole.Items.Count; i++)
            {
                var role = (Role)cmbRole.Items[i];
                if (role.RoleID == user.RoleID)
                {
                    cmbRole.SelectedIndex = i;
                    break;
                }
            }

            if (user.UserWithPermissions != null)
            {
                var permIds = user.UserWithPermissions.Select(uwp => uwp.PermissionsID).ToHashSet();
                for (int i = 0; i < clbPermissions.Items.Count; i++)
                {
                    var p = (Permissions)clbPermissions.Items[i];
                    if (permIds.Contains(p.PermissionsID))
                        clbPermissions.SetItemChecked(i, true);
                }
            }
        }

        private void LoadRoles()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var roles = context.Role.ToList();
                    cmbRole.DataSource = roles;
                    cmbRole.DisplayMember = "RoleName";
                    cmbRole.ValueMember = "RoleID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ролей: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPermissions()
        {
            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var perms = context.Permissions.ToList();
                    clbPermissions.DisplayMember = "PermissionsName";
                    clbPermissions.Items.Clear();
                    foreach (var p in perms)
                        clbPermissions.Items.Add(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки разрешений: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите ФИО", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_editingExistingUser && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите пароль", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Выберите роль", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UserFullName = txtName.Text.Trim();
            UserLoginValue = txtLogin.Text.Trim();
            UserPasswordValue = _editingExistingUser && string.IsNullOrWhiteSpace(txtPassword.Text)
                ? null
                : txtPassword.Text.Trim();
            UserRoleID = (int)cmbRole.SelectedValue;
            SelectedPermissionIds = new List<int>();
            for (int i = 0; i < clbPermissions.CheckedItems.Count; i++)
            {
                var p = (Permissions)clbPermissions.CheckedItems[i];
                SelectedPermissionIds.Add(p.PermissionsID);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
