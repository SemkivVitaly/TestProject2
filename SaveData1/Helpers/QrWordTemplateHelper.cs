using System;
using Microsoft.Office.Interop.Word;

namespace SaveData1.Helpers
{
    /// <summary>Подстановки в шаблон Word при генерации QR (Template.docx).</summary>
    public static class QrWordTemplateHelper
    {
        /// <summary>Заменяет метки «год» (текущий год, yyyy) и «мес» (месяц 01–12) во всём диапазоне.</summary>
        public static void ReplaceDatePlaceholders(Range range)
        {
            if (range == null) return;

            var now = DateTime.Now;
            string year = now.ToString("yyyy");
            string month = now.ToString("MM");

            var findObj = range.Find;
            findObj.ClearFormatting();
            findObj.Replacement.ClearFormatting();
            findObj.MatchCase = false;
            findObj.MatchWholeWord = true;

            findObj.Execute("год", ReplaceWith: year, Replace: WdReplace.wdReplaceAll);
            findObj.Execute("мес", ReplaceWith: month, Replace: WdReplace.wdReplaceAll);
        }
    }
}
