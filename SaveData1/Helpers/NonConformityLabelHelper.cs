using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SaveData1.Entity;
using Word = Microsoft.Office.Interop.Word;

namespace SaveData1.Helpers
{
    /// <summary>Создание записи Error, генерация ярлыка несоответствия по шаблону Word.</summary>
    public static class NonConformityLabelHelper
    {
        private static readonly string[] MonthNames =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        /// <summary>Форматирование даты для ярлыка («день» месяц год).</summary>
        public static string FormatDate(DateTime date)
        {
            return "«" + date.Day + "» " + MonthNames[date.Month - 1] + " " + date.Year + "г.";
        }

        /// <summary>Создание записи Error и возврат её идентификатора.</summary>
        public static int CreateErrorRecord(int? tmId, int? productId, int placeId, DateTime date)
        {
            using (var context = ConnectionHelper.CreateContext())
            {
                var error = new Error
                {
                    TMID = tmId,
                    ProductID = productId,
                    PlaceID = placeId,
                    Date = date,
                    inProgress = false
                };
                context.Error.Add(error);
                context.SaveChanges();
                return error.ErrorID;
            }
        }

        /// <summary>Определение места (сборка/тестирование) по типу записи.</summary>
        public static int DeterminePlaceIdByTmId(int tmId, bool isTesting)
        {
            return isTesting ? 3 : 2;
        }

        /// <summary>Генерация документа ярлыка несоответствия по шаблону Template2.docx.</summary>
        public static void GenerateLabel(
            int errorId,
            string serial,
            string category,
            string placeName,
            string defectText,
            DateTime date,
            string actNumber,
            string fio1,
            string fio2,
            string resultText)
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Template2.docx");
            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Шаблон Template2.docx не найден в папке приложения:\n" + templatePath,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Word документ (*.docx)|*.docx";
                sfd.FileName = "Ярлык_несоответствия_" + errorId + ".docx";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                string savePath = sfd.FileName;
                Word.Application wordApp = null;
                Word.Document doc = null;

                try
                {
                    wordApp = new Word.Application { Visible = false };
                    doc = wordApp.Documents.Open(templatePath, ReadOnly: true);

                    string checkMark = "\u2713";
                    string markI = "";
                    string markR = "";
                    string markN = "";

                    if (!string.IsNullOrEmpty(resultText))
                    {
                        if (resultText.Contains("Изолировано"))
                        {
                            markI = checkMark;
                        }
                        else if (resultText.Contains("доработку") || resultText.Contains("Ремонт"))
                        {
                            markR = checkMark;
                        }
                        else if (resultText.Contains("Отклонение"))
                        {
                            markN = checkMark;
                        }
                    }

                    var replacements = new Dictionary<string, string>
                    {
                        { "{Номер}", errorId.ToString() },
                        { "{Категория}", category ?? "" },
                        { "{Место}", placeName ?? "" },
                        { "{Дефект}", defectText ?? "" },
                        { "{Дата}", FormatDate(date) },
                        { "{Акт}", actNumber ?? "" },
                        { "{Серийный}", serial ?? "" },
                        { "{ФИО1}", fio1 ?? "" },
                        { "{ФИО2}", fio2 ?? "" },
                        { "{И}", markI },
                        { "{Р}", markR },
                        { "{Н}", markN }
                    };

                    foreach (var kvp in replacements)
                    {
                        var find = doc.Content.Find;
                        find.ClearFormatting();
                        find.Replacement.ClearFormatting();
                        find.Execute(kvp.Key, ReplaceWith: kvp.Value, Replace: Word.WdReplace.wdReplaceAll);
                    }

                    doc.SaveAs2(savePath);
                    MessageBox.Show("Ярлык несоответствия сохранён:\n" + savePath,
                        "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try { System.Diagnostics.Process.Start(savePath); }
                    catch { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка генерации ярлыка: " + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (doc != null) { doc.Close(Word.WdSaveOptions.wdDoNotSaveChanges); Marshal.ReleaseComObject(doc); }
                    if (wordApp != null) { wordApp.Quit(); Marshal.ReleaseComObject(wordApp); }
                }
            }
        }

        /// <summary>Текст дефекта из списка описаний через запятую.</summary>
        public static string BuildDefectText(IEnumerable<Description> descriptions)
        {
            return string.Join(", ", descriptions.Select(d => d.DescriptionText));
        }

        /// <summary>Диалог «Сохранить ярлык?» и генерация ярлыка с данными из БД.</summary>
        public static void OfferGenerateLabel(int errorId, string fio1, string defectTextOverride = null)
        {
            var result = MessageBox.Show("Сохранить ярлык несоответствия?", "Ярлык несоответствия",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            using (var context = ConnectionHelper.CreateContext())
            {
                var error = context.Error
                    .Include("Place")
                    .Include("Product")
                    .Include("Product.ProducType")
                    .Include("Product.Act")
                    .Include("TechnicalMapFull")
                    .Include("TechnicalMapFull.Product")
                    .Include("TechnicalMapFull.Product.ProducType")
                    .Include("TechnicalMapFull.Product.Act")
                    .FirstOrDefault(e => e.ErrorID == errorId);
                if (error == null) return;

                string serial = "";
                string category = "";
                string actNumber = "";
                string defectText = defectTextOverride ?? "";

                if (error.TechnicalMapFull != null)
                {
                    var p = error.TechnicalMapFull.Product;
                    serial = p?.ProductSerial ?? "";
                    category = p?.ProducType?.TypeName ?? "";
                    actNumber = p?.Act?.ActNumber ?? "";

                    if (string.IsNullOrEmpty(defectText))
                    {
                        var texts = FaultDescriptionHelper.GetErrorDefectTexts(errorId, error.TMID);
                        defectText = texts != null && texts.Count > 0 ? string.Join(", ", texts.Distinct()) : "";
                    }
                }
                else if (error.Product != null)
                {
                    serial = error.Product.ProductSerial ?? "";
                    category = error.Product.ProducType?.TypeName ?? "";
                    actNumber = error.Product.Act?.ActNumber ?? "";

                    if (string.IsNullOrEmpty(defectText))
                    {
                        var texts = FaultDescriptionHelper.GetErrorDefectTexts(errorId, null);
                        defectText = texts != null && texts.Count > 0 ? string.Join(", ", texts.Distinct()) : "";
                    }
                }

                string placeName = error.Place?.PlaceName ?? "";

                GenerateLabel(errorId, serial, category, placeName,
                    defectText, error.Date, actNumber, fio1, "__________", null);
            }
        }
    }
}
