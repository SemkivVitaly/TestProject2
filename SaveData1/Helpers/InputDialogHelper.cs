using System;
using System.Windows.Forms;

namespace SaveData1.Helpers
{
    public static class InputDialogHelper
    {
        public static string Show(IWin32Window owner, string title, string prompt, string defaultValue = "")
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = owner != null ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
                form.Width = 400;
                form.Height = 150;

                var lbl = new Label
                {
                    Left = 10,
                    Top = 10,
                    Width = 370,
                    Text = prompt,
                    AutoSize = false
                };

                var txt = new TextBox
                {
                    Left = 10,
                    Top = 35,
                    Width = 365,
                    Text = defaultValue
                };

                var btnOk = new Button
                {
                    Text = "OK",
                    Left = 210,
                    Top = 70,
                    Width = 80,
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Left = 295,
                    Top = 70,
                    Width = 80,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btnOk);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                return form.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }
    }
}
