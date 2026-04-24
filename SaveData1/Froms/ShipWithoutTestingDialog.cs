using System;
using System.Drawing;
using System.Windows.Forms;

namespace SaveData1.Froms
{
    /// <summary>
    /// Диалог администратора для отгрузки акта без прохождения тестирования и контроля.
    /// Требует указать причину (минимум 3 символа). Сохраняется в журнал
    /// <c>dbo.ShipmentWithoutTesting</c> через <see cref="Services.ProductLifecycleService.ShipActWithoutTesting"/>.
    /// </summary>
    internal sealed class ShipWithoutTestingDialog : Form
    {
        private readonly TextBox _txtReason;
        private readonly Button _btnOk;

        public string Reason => (_txtReason.Text ?? string.Empty).Trim();

        public ShipWithoutTestingDialog(string actNumber)
        {
            Text = $"Отгрузка без тестирования — акт № {actNumber}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 260);
            ShowInTaskbar = false;

            var lblWarn = new Label
            {
                Location = new Point(12, 12),
                Size = new Size(496, 60),
                Text = "Вы собираетесь отгрузить весь акт на склад БЕЗ тестирования и контроля.\n" +
                       "Укажите причину (минимум 3 символа). Запись попадёт в журнал с указанием " +
                       "администратора, времени и количества отгруженных продуктов.",
                ForeColor = Color.DarkRed
            };

            var lblReason = new Label
            {
                Location = new Point(12, 78),
                Size = new Size(200, 20),
                Text = "Причина отгрузки:"
            };

            _txtReason = new TextBox
            {
                Location = new Point(12, 100),
                Size = new Size(496, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 1000
            };

            _btnOk = new Button
            {
                Location = new Point(332, 215),
                Size = new Size(80, 30),
                Text = "Отгрузить",
                DialogResult = DialogResult.OK,
                Enabled = false
            };
            var btnCancel = new Button
            {
                Location = new Point(418, 215),
                Size = new Size(90, 30),
                Text = "Отмена",
                DialogResult = DialogResult.Cancel
            };

            _txtReason.TextChanged += (s, e) =>
            {
                _btnOk.Enabled = !string.IsNullOrWhiteSpace(_txtReason.Text)
                    && _txtReason.Text.Trim().Length >= 3;
            };

            Controls.AddRange(new Control[] { lblWarn, lblReason, _txtReason, _btnOk, btnCancel });
            AcceptButton = _btnOk;
            CancelButton = btnCancel;
        }
    }
}
