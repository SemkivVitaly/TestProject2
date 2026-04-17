using System;
using System.Text.RegularExpressions;

namespace SaveData1.Helpers
{
    /// <summary>Нормализация и валидация пользовательского ввода: серийник, акт, логин, имя пользователя, путь.</summary>
    public static class InputValidator
    {
        public const int SerialMaxLen = 50;
        public const int ActNumberMaxLen = 6;
        public const int LoginMaxLen = 50;
        public const int UserNameMaxLen = 50;
        public const int PathMaxLen = 250;

        private static readonly Regex ActNumberRegex = new Regex(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled);
        private static readonly Regex LoginRegex = new Regex(@"^[A-Za-z0-9._\-]+$", RegexOptions.Compiled);
        private static readonly Regex SerialRegex = new Regex(@"^[A-Za-z0-9._\-\/]+$", RegexOptions.Compiled);

        public static bool TryNormalizeSerial(string input, out string normalized, out string error)
        {
            normalized = (input ?? "").Trim();
            if (normalized.Length == 0) { error = "Серийный номер пуст."; return false; }
            if (normalized.Length > SerialMaxLen) { error = "Серийный номер длиннее " + SerialMaxLen + " символов."; return false; }
            if (!SerialRegex.IsMatch(normalized)) { error = "Серийный номер содержит недопустимые символы."; return false; }
            error = null;
            return true;
        }

        public static bool TryNormalizeActNumber(string input, out string normalized, out string error)
        {
            normalized = (input ?? "").Trim();
            if (normalized.Length == 0) { error = "Номер акта пуст."; return false; }
            if (normalized.Length > ActNumberMaxLen) { error = "Номер акта длиннее " + ActNumberMaxLen + " символов."; return false; }
            if (!ActNumberRegex.IsMatch(normalized)) { error = "Номер акта содержит недопустимые символы (допускаются буквы, цифры, дефис)."; return false; }
            error = null;
            return true;
        }

        public static bool TryNormalizeLogin(string input, out string normalized, out string error)
        {
            normalized = (input ?? "").Trim();
            if (normalized.Length == 0) { error = "Логин пуст."; return false; }
            if (normalized.Length > LoginMaxLen) { error = "Логин длиннее " + LoginMaxLen + " символов."; return false; }
            if (!LoginRegex.IsMatch(normalized)) { error = "Логин содержит недопустимые символы."; return false; }
            error = null;
            return true;
        }

        public static bool TryNormalizeUserName(string input, out string normalized, out string error)
        {
            normalized = (input ?? "").Trim();
            if (normalized.Length == 0) { error = "ФИО пусто."; return false; }
            if (normalized.Length > UserNameMaxLen) { error = "ФИО длиннее " + UserNameMaxLen + " символов."; return false; }
            error = null;
            return true;
        }

        /// <summary>Проверяет, что путь не пустой и не содержит недопустимых для Windows символов.</summary>
        public static bool TryValidatePath(string input, out string normalized, out string error)
        {
            normalized = (input ?? "").Trim();
            if (normalized.Length == 0) { error = "Путь не задан."; return false; }
            if (normalized.Length > PathMaxLen) { error = "Путь слишком длинный."; return false; }
            foreach (char c in System.IO.Path.GetInvalidPathChars())
            {
                if (normalized.IndexOf(c) >= 0) { error = "Путь содержит недопустимые символы."; return false; }
            }
            error = null;
            return true;
        }

        /// <summary>Безопасное имя компонента (файл/папка) — заменяет недопустимые символы на «_».</summary>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";
            var chars = name.ToCharArray();
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                for (int i = 0; i < chars.Length; i++)
                    if (chars[i] == c) chars[i] = '_';
            }
            string result = new string(chars).Trim();
            return result.Length == 0 ? "_" : result;
        }
    }
}
