namespace SaveData1
{
    partial class SettingsForm
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
            this.lblServer = new System.Windows.Forms.Label();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.chkSqlAuth = new System.Windows.Forms.CheckBox();
            this.lblLogin = new System.Windows.Forms.Label();
            this.txtDbLogin = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtDbPassword = new System.Windows.Forms.TextBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblServer.Location = new System.Drawing.Point(15, 20);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(100, 19);
            this.lblServer.TabIndex = 0;
            this.lblServer.Text = "\u0418\u043c\u044f \u0441\u0435\u0440\u0432\u0435\u0440\u0430:";

            // 
            // txtServerName
            // 
            this.txtServerName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtServerName.Location = new System.Drawing.Point(15, 42);
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(330, 25);
            this.txtServerName.TabIndex = 1;

            // 
            // chkSqlAuth
            // 
            this.chkSqlAuth.AutoSize = true;
            this.chkSqlAuth.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkSqlAuth.Location = new System.Drawing.Point(15, 80);
            this.chkSqlAuth.Name = "chkSqlAuth";
            this.chkSqlAuth.Size = new System.Drawing.Size(250, 23);
            this.chkSqlAuth.TabIndex = 2;
            this.chkSqlAuth.Text = "SQL Server \u0430\u0443\u0442\u0435\u043d\u0442\u0438\u0444\u0438\u043a\u0430\u0446\u0438\u044f";
            this.chkSqlAuth.UseVisualStyleBackColor = true;
            this.chkSqlAuth.CheckedChanged += new System.EventHandler(this.chkSqlAuth_CheckedChanged);

            // 
            // lblLogin
            // 
            this.lblLogin.AutoSize = true;
            this.lblLogin.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblLogin.Location = new System.Drawing.Point(15, 115);
            this.lblLogin.Name = "lblLogin";
            this.lblLogin.Size = new System.Drawing.Size(52, 19);
            this.lblLogin.TabIndex = 3;
            this.lblLogin.Text = "\u041b\u043e\u0433\u0438\u043d:";

            // 
            // txtDbLogin
            // 
            this.txtDbLogin.Enabled = false;
            this.txtDbLogin.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtDbLogin.Location = new System.Drawing.Point(15, 137);
            this.txtDbLogin.Name = "txtDbLogin";
            this.txtDbLogin.Size = new System.Drawing.Size(330, 25);
            this.txtDbLogin.TabIndex = 4;

            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPassword.Location = new System.Drawing.Point(15, 172);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(60, 19);
            this.lblPassword.TabIndex = 5;
            this.lblPassword.Text = "\u041f\u0430\u0440\u043e\u043b\u044c:";

            // 
            // txtDbPassword
            // 
            this.txtDbPassword.Enabled = false;
            this.txtDbPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtDbPassword.Location = new System.Drawing.Point(15, 194);
            this.txtDbPassword.Name = "txtDbPassword";
            this.txtDbPassword.PasswordChar = '*';
            this.txtDbPassword.Size = new System.Drawing.Size(330, 25);
            this.txtDbPassword.TabIndex = 6;

            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnTestConnection.Location = new System.Drawing.Point(15, 235);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(330, 32);
            this.btnTestConnection.TabIndex = 7;
            this.btnTestConnection.Text = "\u0422\u0435\u0441\u0442 \u043f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u044f";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);

            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.Location = new System.Drawing.Point(15, 275);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(330, 20);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // btnSave
            // 
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSave.Location = new System.Drawing.Point(15, 305);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(155, 32);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCancel.Location = new System.Drawing.Point(190, 305);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(155, 32);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "\u041e\u0442\u043c\u0435\u043d\u0430";
            this.btnCancel.UseVisualStyleBackColor = true;

            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(365, 355);
            this.Controls.Add(this.lblServer);
            this.Controls.Add(this.txtServerName);
            this.Controls.Add(this.chkSqlAuth);
            this.Controls.Add(this.lblLogin);
            this.Controls.Add(this.txtDbLogin);
            this.Controls.Add(this.lblPassword);
            this.Controls.Add(this.txtDbPassword);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438 \u043f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u044f";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.CheckBox chkSqlAuth;
        private System.Windows.Forms.Label lblLogin;
        private System.Windows.Forms.TextBox txtDbLogin;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtDbPassword;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
