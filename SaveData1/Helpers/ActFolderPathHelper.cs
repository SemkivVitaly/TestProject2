using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SaveData1.Helpers
{
    /// <summary>
    /// Нормализация путей, связанных с папкой акта вида «Отгрузка_&lt;категория&gt;_Акт_&lt;номер&gt;».
    /// Используется, чтобы не возникали вложенные одинаковые папки при повторном выборе/сохранении.
    /// </summary>
    public static class ActFolderPathHelper
    {
        /// <summary>
        /// Имя папки акта по заданной категории и номеру.
        /// </summary>
        public static string BuildActFolderName(string category, string actNumber)
        {
            string cat = string.IsNullOrWhiteSpace(category) ? "Без_категории" : category.Trim();
            string act = (actNumber ?? "").Trim();
            return $"Отгрузка_{cat}_Акт_{act}";
        }

        /// <summary>
        /// Возвращает базовый путь без финальных сегментов «Отгрузка_&lt;X&gt;_Акт_&lt;Y&gt;».
        /// Идемпотентная: повторный вызов не меняет результат.
        /// </summary>
        public static string StripActSuffix(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            string normalized = path.TrimEnd('\\', '/').TrimEnd();
            var regex = new Regex(@"^Отгрузка_.+?_Акт_.+$", RegexOptions.CultureInvariant);

            // Срезаем все накопившиеся хвостовые сегменты, подпадающие под шаблон.
            while (true)
            {
                string name;
                try
                {
                    name = Path.GetFileName(normalized);
                }
                catch
                {
                    return normalized;
                }

                if (string.IsNullOrEmpty(name) || !regex.IsMatch(name))
                    return normalized;

                string parent = Path.GetDirectoryName(normalized);
                if (string.IsNullOrEmpty(parent) || string.Equals(parent, normalized, StringComparison.OrdinalIgnoreCase))
                    return normalized;

                normalized = parent.TrimEnd('\\', '/').TrimEnd();
            }
        }

        /// <summary>
        /// Безопасно строит путь папки акта. Если переданный <paramref name="basePath"/> уже
        /// завершается фрагментом «Отгрузка_&lt;...&gt;_Акт_&lt;...&gt;», суффикс снимается,
        /// чтобы не возникало вложенности.
        /// </summary>
        public static string BuildActFolderPath(string basePath, string category, string actNumber)
        {
            string cleanBase = StripActSuffix(basePath);
            if (string.IsNullOrWhiteSpace(cleanBase)) cleanBase = basePath ?? "";
            string folderName = BuildActFolderName(category, actNumber);
            return string.IsNullOrWhiteSpace(cleanBase)
                ? folderName
                : Path.Combine(cleanBase, folderName);
        }
    }
}
