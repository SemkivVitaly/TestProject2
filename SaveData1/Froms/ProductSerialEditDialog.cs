using System;
using System.Windows.Forms;

namespace SaveData1
{
    /// <summary>Диалог переименования серийного номера продукта с транслитерацией кириллицы при вводе.</summary>
    public partial class ProductSerialEditDialog : Form
    {
        public string NewSerial => txtSerial.Text.Trim();

        public ProductSerialEditDialog(string currentSerial = "")
        {
            InitializeComponent();
            txtSerial.Text = currentSerial ?? "";
        }

        private static string Transliterate(string text)
        {
            string[] rus = { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я" };
            string[] eng = { "a", "b", "v", "g", "d", "e", "e", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "h", "ts", "ch", "sh", "sch", "", "y", "", "e", "yu", "ya" };
            string result = text.ToLower();
            for (int i = 0; i < rus.Length; i++)
                result = result.Replace(rus[i], eng[i]);
            return result;
        }

        private static string TransliterateOnlyCyrillic(string text)
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

        private static bool IsCyrillic(char c)
        {
            return (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'ё' || c == 'Ё';
        }

        private void txtSerial_KeyPress(object sender, KeyPressEventArgs e)
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

        private void txtSerial_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSerial.Text)) return;
            txtSerial.Text = TransliterateOnlyCyrillic(txtSerial.Text);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSerial.Text))
            {
                MessageBox.Show("Введите серийный номер.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}
