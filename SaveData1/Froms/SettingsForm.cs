using System;
using System.Drawing;
using System.Windows.Forms;
using SaveData1.Helpers;
using SaveData1.Properties;

namespace SaveData1
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            txtServerName.Text = Settings.Default.DbServer;
            chkSqlAuth.Checked = Settings.Default.DbUseSqlAuth;
            txtDbLogin.Text = Settings.Default.DbLogin;
            txtDbPassword.Text = LocalSecretProtector.UnprotectOrPlain(Settings.Default.DbPassword ?? "");
        }

        private void chkSqlAuth_CheckedChanged(object sender, EventArgs e)
        {
            txtDbLogin.Enabled = chkSqlAuth.Checked;
            txtDbPassword.Enabled = chkSqlAuth.Checked;
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            string server = txtServerName.Text.Trim();
            if (string.IsNullOrEmpty(server))
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "Введите имя сервера";
                return;
            }

            try
            {
                ConnectionHelper.TestConnection(
                    server,
                    chkSqlAuth.Checked,
                    txtDbLogin.Text.Trim(),
                    txtDbPassword.Text);

                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = "Подключение успешно!";
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "Ошибка: " + ex.Message;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string server = txtServerName.Text.Trim();
            if (string.IsNullOrEmpty(server))
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "Введите имя сервера";
                return;
            }

            Settings.Default.DbServer = server;
            Settings.Default.DbUseSqlAuth = chkSqlAuth.Checked;
            Settings.Default.DbLogin = txtDbLogin.Text.Trim();
            Settings.Default.DbPassword = string.IsNullOrEmpty(txtDbPassword.Text)
                ? ""
                : LocalSecretProtector.Protect(txtDbPassword.Text);
            Settings.Default.Save();

            MessageBox.Show("Настройки сохранены.", "Настройки",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
