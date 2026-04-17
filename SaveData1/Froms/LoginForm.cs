using System;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SaveData1.Entity;
using SaveData1.Helpers;
using SaveData1.Services;

namespace SaveData1
{
    public partial class LoginForm : Form
    {
        private Label _lblCapsLock;
        private Label _lblVersion;

        public LoginForm()
        {
            InitializeComponent();
            InitializeExtraUi();
        }

        private void InitializeExtraUi()
        {
            _lblCapsLock = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.DarkOrange,
                Location = new Point(150, 117),
                Text = "Caps Lock включён",
                Visible = false
            };
            panelMain.Controls.Add(_lblCapsLock);

            _lblVersion = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Text = "v. " + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _lblVersion.Location = new Point(ClientSize.Width - _lblVersion.PreferredWidth - 8, ClientSize.Height - 20);
            Controls.Add(_lblVersion);
            _lblVersion.BringToFront();

            txtPassword.KeyUp += (s, e) => UpdateCapsLockIndicator();
            txtPassword.GotFocus += (s, e) => UpdateCapsLockIndicator();
            txtPassword.LostFocus += (s, e) => { _lblCapsLock.Visible = false; };
        }

        private void UpdateCapsLockIndicator()
        {
            _lblCapsLock.Visible = Control.IsKeyLocked(Keys.CapsLock) && txtPassword.Focused;
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
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Введите логин и пароль";
                return;
            }

            try
            {
                var user = UserPermissionsService.FindByLogin(login);

                if (user == null || !PasswordHasher.Verify(password, user.UserPassword))
                {
                    lblError.Text = "Неверный логин или пароль";
                    return;
                }

                if (!PasswordHasher.IsHashedFormat(user.UserPassword))
                {
                    try
                    {
                        int userId = user.UserID;
                        string newHash = PasswordHasher.HashPassword(password);
                        DbOperation.Execute(ctx =>
                        {
                            var tracked = ctx.UsersProfile.Find(userId);
                            if (tracked != null)
                            {
                                tracked.UserPassword = newHash;
                                ctx.SaveChanges();
                            }
                        }, "LoginForm.RehashPassword");
                        user.UserPassword = newHash;
                    }
                    catch (Exception migEx)
                    {
                        AppLog.Warn("Не удалось перехешировать пароль пользователя UserID=" + user.UserID + ". Вход выполнен, миграция хэша пропущена.", migEx);
                    }
                }

                var flags = UserPermissionsService.GetFlags(user);

                Form nextForm;
                if (flags.IsStorage)
                {
                    nextForm = new WarehouseForm(user);
                }
                else if (flags.CanUseEmployeeForm)
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
            catch (Exception ex)
            {
                ExceptionDisplay.ShowError(this, ex, "Ошибка подключения к базе данных");
            }
        }
    }
}
