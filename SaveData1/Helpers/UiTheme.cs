using System.Drawing;
using System.Windows.Forms;

namespace SaveData1.Helpers
{
    /// <summary>Единый визуальный стиль приложения: шрифт и палитра для статуса.</summary>
    public static class UiTheme
    {
        public static readonly Font DefaultFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static readonly Font BoldFont = new Font("Segoe UI", 9.75F, FontStyle.Bold);
        public static readonly Font MonoFont = new Font("Consolas", 9F, FontStyle.Regular);

        public static readonly Color Success = Color.FromArgb(76, 175, 80);
        public static readonly Color Warning = Color.FromArgb(255, 152, 0);
        public static readonly Color Error = Color.FromArgb(244, 67, 54);
        public static readonly Color Info = Color.FromArgb(33, 150, 243);
        public static readonly Color Muted = Color.FromArgb(120, 120, 120);

        /// <summary>Применить базовый шрифт и нестандартные цвета в пределах формы (рекурсивно).</summary>
        public static void Apply(Control root)
        {
            if (root == null) return;
            root.Font = DefaultFont;
            foreach (Control c in root.Controls)
            {
                Apply(c);
            }
        }

        /// <summary>Сделать кнопку «основной» (accent-цвет), например для Save.</summary>
        public static void Primary(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(0, 122, 204);
            btn.ForeColor = Color.White;
            btn.Font = BoldFont;
            btn.FlatAppearance.BorderSize = 0;
        }

        /// <summary>Сделать кнопку «опасной» (красной) — Удалить, Сбросить.</summary>
        public static void Danger(Button btn)
        {
            if (btn == null) return;
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Error;
            btn.ForeColor = Color.White;
            btn.Font = BoldFont;
            btn.FlatAppearance.BorderSize = 0;
        }
    }
}
