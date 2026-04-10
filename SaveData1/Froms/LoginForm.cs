using System;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;

namespace SaveData1
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.PasswordChar = chkShowPassword.Checked ? '\0' : '*';
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.ShowDialog(this);
            }
        }

        private void btnObsSettings_Click(object sender, EventArgs e)
        {
            string ip = "127.0.0.1";
            int port = 4455;
            string password = "";
            if (ObsConfig.IsConfigured())
                ObsConfig.Load(out ip, out port, out password);

            using (var form = new Form())
            {
                form.Text = "Настройки OBS WebSocket";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new Size(340, 230);
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblIp = new Label { Text = "IP адрес:", Location = new Point(15, 20), AutoSize = true, Font = new Font("Segoe UI", 10F) };
                var txtIp = new TextBox { Text = ip, Location = new Point(15, 42), Size = new Size(310, 25), Font = new Font("Segoe UI", 10F) };

                var lblPort = new Label { Text = "Порт:", Location = new Point(15, 75), AutoSize = true, Font = new Font("Segoe UI", 10F) };
                var txtPort = new TextBox { Text = port.ToString(), Location = new Point(15, 97), Size = new Size(310, 25), Font = new Font("Segoe UI", 10F) };

                var lblPwd = new Label { Text = "Пароль:", Location = new Point(15, 130), AutoSize = true, Font = new Font("Segoe UI", 10F) };
                var txtPwd = new TextBox { Text = password, Location = new Point(15, 152), Size = new Size(310, 25), Font = new Font("Segoe UI", 10F), PasswordChar = '*' };

                var btnTest = new Button { Text = "Проверить", Location = new Point(15, 190), Size = new Size(100, 28) };
                var btnSave = new Button { Text = "Сохранить", Location = new Point(125, 190), Size = new Size(100, 28) };
                var btnClear = new Button { Text = "Сбросить", Location = new Point(235, 190), Size = new Size(90, 28) };

                btnTest.Click += (s, ev) =>
                {
                    int p;
                    if (!int.TryParse(txtPort.Text.Trim(), out p)) { MessageBox.Show("Неверный порт.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    using (var obs = new ObsWebSocketHelper(5000))
                    {
                        bool ok = obs.Connect(txtIp.Text.Trim(), p, txtPwd.Text);
                        if (ok)
                            MessageBox.Show("Подключение к OBS успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Не удалось подключиться к OBS.\nУбедитесь, что OBS запущен и WebSocket-сервер включён.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                btnSave.Click += (s, ev) =>
                {
                    int p;
                    if (!int.TryParse(txtPort.Text.Trim(), out p)) { MessageBox.Show("Неверный порт.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    ObsConfig.Save(txtIp.Text.Trim(), p, txtPwd.Text);
                    MessageBox.Show("Настройки OBS сохранены.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };

                btnClear.Click += (s, ev) =>
                {
                    ObsConfig.Delete();
                    txtIp.Text = "127.0.0.1";
                    txtPort.Text = "4455";
                    txtPwd.Text = "";
                    MessageBox.Show("Настройки OBS удалены.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                form.Controls.AddRange(new Control[] { lblIp, txtIp, lblPort, txtPort, lblPwd, txtPwd, btnTest, btnSave, btnClear });
                form.ShowDialog(this);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Введите логин и пароль";
                return;
            }

            try
            {
                using (var context = ConnectionHelper.CreateContext())
                {
                    var user = context.UsersProfile
                        .AsNoTracking()
                        .Include("Role")
                        .Include("UserWithPermissions.Permissions")
                        .FirstOrDefault<UsersProfile>(u => u.UserLogin == login);

                    if (user == null || !PasswordHasher.Verify(password, user.UserPassword))
                    {
                        lblError.Text = "Неверный логин или пароль";
                        return;
                    }

                    if (!PasswordHasher.IsHashedFormat(user.UserPassword))
                    {
                        try
                        {
                            var tracked = context.UsersProfile.Find(user.UserID);
                            if (tracked != null)
                            {
                                tracked.UserPassword = PasswordHasher.HashPassword(password);
                                context.SaveChanges();
                                user.UserPassword = tracked.UserPassword;
                            }
                        }
                        catch { /* вход уже успешен, миграция хэша не критична */ }
                    }

                    bool hasAssembly = user.UserWithPermissions.Any(p => p.Permissions.PermissionsName == "Сборщик");
                    bool hasTesting = user.UserWithPermissions.Any(p => p.Permissions.PermissionsName == "Тестировщик");
                    bool hasInspection = user.UserWithPermissions.Any(p => p.Permissions.PermissionsName == "Инспектор");
                    bool hasStorage = user.Role.RoleName == "Storage";
                    bool hasAdmin = user.Role.RoleName == "Admin";

                    Form nextForm = null;

                    if (hasStorage)
                    {
                        nextForm = new WarehouseForm(user);
                    }
                    else if (hasAssembly || hasTesting || hasInspection || hasAdmin)
                    {
                        nextForm = new EmployeeForm(user);
                    }
                    else
                    {
                        lblError.Text = "Нет доступных разрешений";
                        return;
                    }

                    this.Hide();
                    nextForm.FormClosed += (s, args) =>
                    {
                        this.Show();
                        txtPassword.Clear();
                        lblError.Text = "";
                    };
                    nextForm.Show();
                }
            }
            catch (Exception ex)
            {
                string inner = ex.InnerException != null ? "\n\nПодробнее: " + ex.InnerException.Message : "";
                MessageBox.Show("Ошибка подключения к базе данных:\n" + ex.Message + inner,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
