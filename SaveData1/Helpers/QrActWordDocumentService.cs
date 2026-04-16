using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SaveData1.Entity;
using Word = Microsoft.Office.Interop.Word;

namespace SaveData1.Helpers
{
    /// <summary>
    /// Генерация Word-документа с QR по акту из шаблона <see cref="QrTemplateFileName"/> (форма сотрудника и склад).
    /// </summary>
    public static class QrActWordDocumentService
    {
        public const string QrTemplateFileName = "Template.docx";

        public static string GetTemplateFullPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, QrTemplateFileName);
        }

        /// <summary>Если шаблона нет — предлагает скопировать файл с диска.</summary>
        /// <returns>false, если пользователь отменил выбор файла.</returns>
        public static bool TryEnsureQrTemplateExists(IWin32Window owner)
        {
            string templatePath = GetTemplateFullPath();
            if (File.Exists(templatePath))
                return true;

            MessageBox.Show(
                owner,
                "Файл шаблона '" + QrTemplateFileName + "' не найден в папке с программой.\n\n" +
                "Пожалуйста, выберите ваш файл-шаблон (Пр Godex ОБРАЗЕЦ.docx). Он будет скопирован в папку с программой для дальнейшего автоматического использования.",
                "Шаблон не найден",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Выберите файл-шаблон Word (Пр Godex ОБРАЗЕЦ.docx)";
                ofd.Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*";
                if (ofd.ShowDialog(owner) != DialogResult.OK)
                    return false;

                try
                {
                    File.Copy(ofd.FileName, templatePath, true);
                    MessageBox.Show(
                        owner,
                        "Шаблон успешно сохранен! Теперь он будет использоваться автоматически.",
                        "Успех",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        owner,
                        "Не удалось скопировать шаблон: " + ex.Message,
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        /// <summary>Диалог «Сохранить как…» для итогового docx.</summary>
        /// <returns>Полный путь или null при отмене.</returns>
        public static string PromptQrOutputPath(IWin32Window owner, string actNumber)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Сохранить итоговый документ с QR-кодами как...";
                sfd.Filter = "Word Documents (*.docx)|*.docx";
                sfd.FileName = $"Акт_№{actNumber}_QR.docx";
                return sfd.ShowDialog(owner) == DialogResult.OK ? sfd.FileName : null;
            }
        }

        /// <summary>
        /// Собирает документ: подстановки даты/акта/серийника и вставка QR в каждом блоке.
        /// </summary>
        /// <returns>Число вставленных QR (продукты без серийного номера пропускаются).</returns>
        public static int GenerateActQrWordDocument(string templatePath, string outputPath, string actNumber, IReadOnlyList<Product> products)
        {
            if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
                throw new FileNotFoundException("Шаблон Word не найден.", templatePath);
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Путь сохранения не задан.", nameof(outputPath));
            if (products == null || products.Count == 0)
                throw new ArgumentException("Список продуктов пуст.", nameof(products));

            Word.Application wordApp = null;
            Word.Document finalDoc = null;

            try
            {
                wordApp = new Word.Application { Visible = false };
                wordApp.Options.SmartCutPaste = false;

                finalDoc = wordApp.Documents.Add(templatePath);
                finalDoc.Content.Copy();

                int generated = 0;
                for (int i = 0; i < products.Count; i++)
                {
                    Product product = products[i];
                    string serial = product.ProductSerial;
                    if (string.IsNullOrEmpty(serial))
                        continue;

                    string tempImg = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
                    QrCodeHelper.SaveQrCode(serial, tempImg);

                    Word.Range workRange;
                    if (i == 0)
                    {
                        workRange = finalDoc.Content;
                    }
                    else
                    {
                        Word.Range pasteRange = finalDoc.Content;
                        pasteRange.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
                        pasteRange.Text = "\r";
                        pasteRange.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
                        int start = pasteRange.Start;
                        pasteRange.Paste();
                        int end = finalDoc.Content.End;
                        workRange = finalDoc.Range(start, end);
                        RemoveLeadingParagraphFromPreviousLineIfAtPageStart(finalDoc, workRange);
                        RemoveLeadingEmptyParagraph(workRange);
                    }

                    Word.Find findObj = workRange.Find;
                    findObj.ClearFormatting();
                    findObj.Replacement.ClearFormatting();
                    QrWordTemplateHelper.ReplaceDatePlaceholders(workRange);

                    findObj = workRange.Find;
                    findObj.ClearFormatting();
                    findObj.Replacement.ClearFormatting();
                    findObj.Execute("993", ReplaceWith: actNumber, Replace: Word.WdReplace.wdReplaceAll);
                    findObj.Execute("Серийный", ReplaceWith: serial, Replace: Word.WdReplace.wdReplaceAll);

                    Word.Find findQr = workRange.Find;
                    findQr.ClearFormatting();
                    findQr.Text = "QR код";
                    if (findQr.Execute())
                    {
                        Word.Range rangeQr = findQr.Parent;
                        rangeQr.Text = "";
                        object linkToFile = false;
                        object saveWithDoc = true;
                        rangeQr.InlineShapes.AddPicture(tempImg, ref linkToFile, ref saveWithDoc);
                    }

                    if (File.Exists(tempImg))
                        File.Delete(tempImg);

                    generated++;
                }

                TrimTrailingBlankParagraph(finalDoc);
                finalDoc.SaveAs2(outputPath);
                return generated;
            }
            finally
            {
                if (finalDoc != null)
                {
                    finalDoc.Close(Word.WdSaveOptions.wdDoNotSaveChanges);
                    Marshal.FinalReleaseComObject(finalDoc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit();
                    Marshal.FinalReleaseComObject(wordApp);
                }
            }
        }

        private static void TrimTrailingBlankParagraph(Word.Document doc)
        {
            try
            {
                Word.Range endRange = doc.Content;
                endRange.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
                if (endRange.Start > 0)
                {
                    endRange.MoveStart(Word.WdUnits.wdCharacter, -1);
                    string lastCh = endRange.Text ?? "";
                    if (lastCh.Length == 1 && (lastCh[0] == '\r' || lastCh[0] == '\n' || lastCh[0] == (char)13 || lastCh[0] == (char)10))
                        endRange.Text = "";
                }
            }
            catch
            {
                /* намеренно игнорируем */
            }
        }

        private static void RemoveLeadingEmptyParagraph(Word.Range workRange)
        {
            if (workRange == null || workRange.Start >= workRange.End) return;
            const int maxRemove = 20;
            for (int i = 0; i < maxRemove; i++)
            {
                if (workRange.Start >= workRange.End) return;
                Word.Range first = workRange.Duplicate;
                first.Collapse(Word.WdCollapseDirection.wdCollapseStart);
                first.MoveEnd(Word.WdUnits.wdCharacter, 1);
                string t = first.Text ?? "";
                if (t.Length != 1) return;
                char c = t[0];
                if (c != '\r' && c != '\n' && c != (char)13 && c != (char)10) return;
                first.Text = "";
            }
        }

        private static void RemoveLeadingParagraphFromPreviousLineIfAtPageStart(Word.Document doc, Word.Range workRange)
        {
            if (doc == null || workRange == null || workRange.Start <= 1 || workRange.Start >= workRange.End) return;
            try
            {
                Word.Range firstChar = workRange.Duplicate;
                firstChar.Collapse(Word.WdCollapseDirection.wdCollapseStart);
                firstChar.MoveEnd(Word.WdUnits.wdCharacter, 1);
                string firstText = firstChar.Text ?? "";
                if (firstText.Length != 1) return;
                char c = firstText[0];
                if (c != '\r' && c != '\n' && c != (char)13 && c != (char)10) return;

                Word.Range beforeRange = doc.Range(workRange.Start - 1, workRange.Start);
                string t = beforeRange.Text ?? "";
                if (t.Length != 1 || (t[0] != '\r' && t[0] != '\n' && t[0] != (char)13 && t[0] != (char)10)) return;
                int pageAtStart = (int)workRange.Information[Word.WdInformation.wdActiveEndPageNumber];
                Word.Range prevRange = doc.Range(workRange.Start - 1, workRange.Start - 1);
                int pageBefore = (int)prevRange.Information[Word.WdInformation.wdActiveEndPageNumber];
                if (pageAtStart > pageBefore) return;
                beforeRange.Text = "";
            }
            catch
            {
                /* намеренно игнорируем */
            }
        }
    }
}
