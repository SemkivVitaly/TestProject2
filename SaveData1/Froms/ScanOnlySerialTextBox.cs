using System.Windows.Forms;

namespace SaveData1
{
    /// <summary>Поле только для сканера (эмуляция клавиатуры): вставка из буфера и горячие клавиши вставки отключены.</summary>
    public class ScanOnlySerialTextBox : TextBox
    {
        private const int WM_PASTE = 0x0302;

        public ScanOnlySerialTextBox()
        {
            ShortcutsEnabled = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PASTE)
                return;
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.V) || keyData == (Keys.Control | Keys.Shift | Keys.V))
                return true;
            if (keyData == (Keys.Shift | Keys.Insert))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
