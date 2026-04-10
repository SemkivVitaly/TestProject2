using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;

namespace SaveData1.Helpers
{
    /// <summary>Создание и обновление Excel-отчёта "Отчет_Акт_№ Акта".</summary>
    public static class ExcelReportHelper
    {
        private const string SuccessMarker = "Test COMPLETE: FCBOARD OK.";
        private const string SheetActs = "Акты";
        private const string SheetRepair = "Ремонт";

        private static readonly int ColorPastelBlue = 245 * 65536 + 235 * 256 + 220;
        private static readonly int ColorPastelGreen = 230 * 65536 + 245 * 256 + 220;
        private static readonly int ColorPastelYellow = 230 * 65536 + 250 * 256 + 255;

        private static readonly string[] ActsHeaders = new[]
        {
            "№", "Дата", "Производитель", "Исполнитель", "S/N платы", "S/N IMU",
            "№ стенда", "№ Теста", "Визуальный осмотр", "Ударная нагрузка", "FC1_питание",
            "FC2_питание", "+5В_FC1", "+5В_FC2", "FC_тест пройден",
            "Внешние датчики_тест пройден", "Длительный тест пройден", "Итог", "Примечание"
        };

        private static readonly string[] RepairHeaders = new[]
        {
            "№", "№ Акта", "Дата", "Производитель", "Исполнитель", "S/N платы", "S/N IMU",
            "№ стенда", "№ Теста", "Визуальный осмотр", "Ударная нагрузка", "FC1_питание",
            "FC2_питание", "+5В_FC1", "+5В_FC2", "FC_тест пройден",
            "Внешние датчики_тест пройден", "Длительный тест пройден", "Итог", "Примечание"
        };

        /// <summary>Проверяет, содержит ли txt-файл строку успешного теста.</summary>
        public static bool ContainsSuccessMarker(string txtFilePath)
        {
            if (string.IsNullOrEmpty(txtFilePath) || !File.Exists(txtFilePath))
                return false;

            try
            {
                string content = File.ReadAllText(txtFilePath);
                return content.Contains(SuccessMarker);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Ищет первый txt-файл с маркером успеха в папке.</summary>
        public static string FindFirstTxtWithSuccess(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return null;

            foreach (string path in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".log", StringComparison.OrdinalIgnoreCase)))
            {
                if (ContainsSuccessMarker(path))
                    return path;
            }
            return null;
        }

        /// <summary>Возвращает путь к первому txt-файлу в папке (для открытия при ошибке).</summary>
        public static string GetFirstTxtFile(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return null;

            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return files.Length > 0 ? files[0] : null;
        }

        /// <summary>Проверяет, есть ли в папке хотя бы один txt с маркером успеха.</summary>
        public static bool FolderHasSuccess(string folderPath)
        {
            return FindFirstTxtWithSuccess(folderPath) != null;
        }

        /// <summary>Получает путь к файлу отчёта.</summary>
        public static string GetReportPath(string rootFolder, string actNumber)
        {
            string fileName = $"Отчет_Акт_{actNumber.Trim()}.xlsx";
            return Path.Combine(rootFolder, fileName);
        }

        /// <summary>Проверяет, есть ли серийный номер в отчёте (листы Акты и Ремонт).</summary>
        public static bool SerialExistsInReport(string reportPath, string serialNumber)
        {
            if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath) || string.IsNullOrWhiteSpace(serialNumber))
                return false;

            Application app = null;
            Workbook wb = null;

            try
            {
                app = new Application { Visible = false, DisplayAlerts = false };
                wb = app.Workbooks.Open(reportPath);
                string serial = serialNumber.Trim();

                foreach (string sheetName in new[] { SheetActs, SheetRepair })
                {
                    Worksheet ws = FindSheet(wb, sheetName);
                    if (ws == null) continue;

                    int serialCol = sheetName.Equals(SheetRepair, StringComparison.OrdinalIgnoreCase) ? 6 : 5;
                    int lastRow = ((Range)ws.Cells[ws.Rows.Count, serialCol]).End[XlDirection.xlUp].Row;
                    int startRow = sheetName.Equals(SheetRepair, StringComparison.OrdinalIgnoreCase) ? 2 : 3;

                    for (int r = startRow; r <= lastRow; r++)
                    {
                        object val = ((Range)ws.Cells[r, serialCol]).Value;
                        if (val != null && string.Equals(val.ToString().Trim(), serial, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (wb != null)
                {
                    wb.Close(false);
                    Marshal.ReleaseComObject(wb);
                }
                if (app != null)
                {
                    app.Quit();
                    Marshal.ReleaseComObject(app);
                }
            }
        }

        /// <summary>Добавляет запись на лист "Акты" (успешный тест).</summary>
        public static void AddActEntry(string reportPath, string actNumber, string performer, string serialNumber,
            string standNumber, int nextRowNumber)
        {
            Application app = null;
            Workbook wb = null;

            try
            {
                app = new Application { Visible = false, DisplayAlerts = false };
                wb = File.Exists(reportPath)
                    ? app.Workbooks.Open(reportPath)
                    : CreateNewWorkbook(app, reportPath, actNumber);

                Worksheet wsActs = GetOrCreateSheet(wb, SheetActs, true);
                EnsureHeaders(wsActs, ActsHeaders, true, actNumber);

                int dataStartRow = GetNextDataStartRow(wsActs, 3);

                string dateStr = DateTime.Now.ToString("dd.MM.yyyy");

                WriteActRow(wsActs, dataStartRow, nextRowNumber, dateStr, performer, serialNumber,
                    standNumber, 1, "+", "+", "+", "+", "+", "+", "+", "-", "+", "Т");

                WriteActRow(wsActs, dataStartRow + 1, nextRowNumber, dateStr, performer, serialNumber,
                    standNumber, 2, "-", "-", "-", "-", "-", "-", "-", "-", "-", "-");

                MergeCommonCells(wsActs, dataStartRow, dataStartRow + 1, true);
                FormatDataRows(wsActs, dataStartRow, dataStartRow + 1, true, 19);

                wb.Save();
            }
            finally
            {
                if (wb != null)
                {
                    wb.Close(false);
                    Marshal.ReleaseComObject(wb);
                }
                if (app != null)
                {
                    app.Quit();
                    Marshal.ReleaseComObject(app);
                }
            }
        }

        /// <summary>Добавляет запись на лист "Ремонт" (тест с ошибкой).</summary>
        public static void AddRepairEntry(string reportPath, string actNumber, string performer, string serialNumber,
            string standNumber, int nextRowNumber)
        {
            Application app = null;
            Workbook wb = null;

            try
            {
                app = new Application { Visible = false, DisplayAlerts = false };
                wb = File.Exists(reportPath)
                    ? app.Workbooks.Open(reportPath)
                    : CreateNewWorkbook(app, reportPath, actNumber);

                Worksheet wsRepair = GetOrCreateSheet(wb, SheetRepair, false);
                EnsureHeaders(wsRepair, RepairHeaders, false, actNumber);

                int dataStartRow = GetNextDataStartRow(wsRepair, 2);

                string dateStr = DateTime.Now.ToString("dd.MM.yyyy");

                WriteRepairRow(wsRepair, dataStartRow, nextRowNumber, actNumber, dateStr, performer, serialNumber,
                    standNumber, 1, "+", "+", "", "", "", "", "", "", "", "");

                WriteRepairRow(wsRepair, dataStartRow + 1, nextRowNumber, actNumber, dateStr, performer, serialNumber,
                    "", 2, "+", "+", "", "", "", "", "", "", "", "");

                MergeCommonCells(wsRepair, dataStartRow, dataStartRow + 1, false);
                FormatDataRows(wsRepair, dataStartRow, dataStartRow + 1, false, 20);

                wb.Save();
            }
            finally
            {
                if (wb != null)
                {
                    wb.Close(false);
                    Marshal.ReleaseComObject(wb);
                }
                if (app != null)
                {
                    app.Quit();
                    Marshal.ReleaseComObject(app);
                }
            }
        }

        public const string SheetActsName = "Акты";
        public const string SheetRepairName = "Ремонт";

        private static int GetNextDataStartRow(Worksheet ws, int firstDataRow)
        {
            int lastRow = ((Range)ws.Cells[ws.Rows.Count, 8]).End[XlDirection.xlUp].Row;
            if (lastRow < firstDataRow) return firstDataRow;
            return lastRow + 1;
        }

        /// <summary>Получает следующий номер записи (макс № + 1) на указанном листе.</summary>
        public static int GetNextRowNumber(string reportPath, string sheetName)
        {
            if (string.IsNullOrEmpty(reportPath) || !File.Exists(reportPath))
                return 1;

            Application app = null;
            Workbook wb = null;

            try
            {
                app = new Application { Visible = false, DisplayAlerts = false };
                wb = app.Workbooks.Open(reportPath);
                Worksheet ws = FindSheet(wb, sheetName);
                if (ws == null) return 1;

                int startRow = sheetName.Equals(SheetRepair, StringComparison.OrdinalIgnoreCase) ? 2 : 3;
                int lastRow = ((Range)ws.Cells[ws.Rows.Count, 8]).End[XlDirection.xlUp].Row;
                if (lastRow < startRow) return 1;

                int maxNum = 0;
                for (int r = startRow; r <= lastRow; r += 2)
                {
                    object val = ((Range)ws.Cells[r, 1]).Value;
                    if (val != null && int.TryParse(val.ToString(), out int n) && n > maxNum)
                        maxNum = n;
                }
                return maxNum + 1;
            }
            catch
            {
                return 1;
            }
            finally
            {
                if (wb != null)
                {
                    wb.Close(false);
                    Marshal.ReleaseComObject(wb);
                }
                if (app != null)
                {
                    app.Quit();
                    Marshal.ReleaseComObject(app);
                }
            }
        }

        private static Workbook CreateNewWorkbook(Application app, string filePath, string actNumber)
        {
            Workbook wb = app.Workbooks.Add();
            Worksheet ws1 = (Worksheet)wb.Sheets[1];
            ws1.Name = SheetActs;
            EnsureHeaders(ws1, ActsHeaders, true, actNumber);
            SetColumnWidths(ws1, 19);

            Worksheet ws2 = (Worksheet)wb.Sheets.Add(After: ws1);
            ws2.Name = SheetRepair;
            EnsureHeaders(ws2, RepairHeaders, false, actNumber);
            SetColumnWidths(ws2, 20);

            wb.SaveAs(filePath);
            return wb;
        }

        private static void SetColumnWidths(Worksheet ws, int colCount)
        {
            double[] widthsActs = { 6, 12, 14, 16, 12, 10, 10, 8, 14, 14, 12, 12, 10, 10, 14, 22, 18, 8, 14 };
            double[] widthsRepair = { 6, 10, 12, 14, 16, 12, 10, 10, 8, 14, 14, 12, 12, 10, 10, 14, 22, 18, 8, 14 };
            double[] widths = colCount == 20 ? widthsRepair : widthsActs;
            for (int c = 1; c <= widths.Length && c <= colCount; c++)
            {
                Range col = (Range)ws.Cells[1, c];
                col.ColumnWidth = widths[c - 1];
                Marshal.ReleaseComObject(col);
            }
        }

        private static Worksheet GetOrCreateSheet(Workbook wb, string sheetName, bool preferFirst)
        {
            Worksheet ws = FindSheet(wb, sheetName);
            if (ws != null) return ws;

            if (preferFirst)
            {
                ws = (Worksheet)wb.Sheets[1];
                ws.Name = sheetName;
                return ws;
            }

            ws = (Worksheet)wb.Sheets.Add(After: wb.Sheets[wb.Sheets.Count]);
            ws.Name = sheetName;
            return ws;
        }

        private static Worksheet FindSheet(Workbook wb, string sheetName)
        {
            foreach (Worksheet ws in wb.Sheets)
            {
                if (string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                    return ws;
            }
            return null;
        }

        private static void EnsureHeaders(Worksheet ws, string[] headers, bool isActsSheet, string actNumber)
        {
            object val = ((Range)ws.Cells[1, 1]).Value;
            if (val != null && !string.IsNullOrWhiteSpace(val.ToString())) return;

            for (int c = 0; c < headers.Length; c++)
                ((Range)ws.Cells[1, c + 1]).Value = headers[c];

            FormatHeaderRow(ws, 1, headers.Length, isActsSheet);

            if (isActsSheet)
            {
                Range actRow = ws.Range[ws.Cells[2, 1], ws.Cells[2, headers.Length]];
                actRow.Merge();
                actRow.Value = $"Акт № {actNumber}";
                actRow.HorizontalAlignment = XlHAlign.xlHAlignCenter;
                actRow.VerticalAlignment = XlVAlign.xlVAlignCenter;
                actRow.Font.Bold = true;
                actRow.Font.Size = 10;
                actRow.Font.Name = "Calibri";
                actRow.Interior.Color = ColorPastelYellow;
                ApplyAllBordersAndAlignment(actRow);
                actRow.RowHeight = 25;
                Marshal.ReleaseComObject(actRow);
            }
        }

        private static void FormatHeaderRow(Worksheet ws, int row, int colCount, bool isActsSheet)
        {
            Range usedRange = ws.Range[ws.Cells[row, 1], ws.Cells[row, colCount]];
            ApplyAllBordersAndAlignment(usedRange);
            ApplyHeaderColors(ws, row, colCount, isActsSheet);
            usedRange.Font.Bold = true;
            usedRange.Font.Size = 10;
            usedRange.Font.Name = "Calibri";
            usedRange.WrapText = true;
            usedRange.RowHeight = 30;
            Marshal.ReleaseComObject(usedRange);
        }

        private static void ApplyAllBordersAndAlignment(Range rng)
        {
            rng.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = XlLineStyle.xlContinuous;
            rng.Borders[XlBordersIndex.xlEdgeTop].LineStyle = XlLineStyle.xlContinuous;
            rng.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = XlLineStyle.xlContinuous;
            rng.Borders[XlBordersIndex.xlEdgeRight].LineStyle = XlLineStyle.xlContinuous;
            try
            {
                rng.Borders[XlBordersIndex.xlInsideHorizontal].LineStyle = XlLineStyle.xlContinuous;
                rng.Borders[XlBordersIndex.xlInsideVertical].LineStyle = XlLineStyle.xlContinuous;
            }
            catch { }
            rng.HorizontalAlignment = XlHAlign.xlHAlignCenter;
            rng.VerticalAlignment = XlVAlign.xlVAlignCenter;
            rng.Font.Size = 10;
            rng.Font.Name = "Calibri";
        }

        private static void ApplyHeaderColors(Worksheet ws, int row, int colCount, bool isActsSheet)
        {
            int[] pastelGreenColsActs = { 1, 5, 6, 8, 16, 18 };
            int[] pastelBlueColsActs = { 2, 3, 4, 7, 9, 10, 11, 12, 13, 14, 15, 17, 19 };
            int[] pastelGreenColsRepair = { 1, 2, 6, 7, 9, 17, 19 };
            int[] pastelBlueColsRepair = { 3, 4, 5, 8, 10, 11, 12, 13, 14, 15, 16, 18, 20 };
            int[] pastelGreenCols = isActsSheet ? pastelGreenColsActs : pastelGreenColsRepair;
            int[] pastelBlueCols = isActsSheet ? pastelBlueColsActs : pastelBlueColsRepair;

            foreach (int c in pastelGreenCols)
            {
                if (c <= colCount)
                {
                    Range cell = (Range)ws.Cells[row, c];
                    cell.Interior.Color = ColorPastelGreen;
                    Marshal.ReleaseComObject(cell);
                }
            }
            foreach (int c in pastelBlueCols)
            {
                if (c <= colCount)
                {
                    Range cell = (Range)ws.Cells[row, c];
                    cell.Interior.Color = ColorPastelBlue;
                    Marshal.ReleaseComObject(cell);
                }
            }
        }

        private static void FormatDataRows(Worksheet ws, int row1, int row2, bool isActsSheet, int colCount)
        {
            for (int r = row1; r <= row2; r++)
            {
                Range rowRange = (Range)ws.Rows[r];
                rowRange.RowHeight = 22;
                Marshal.ReleaseComObject(rowRange);
                for (int c = 1; c <= colCount; c++)
                {
                    Range cell = (Range)ws.Cells[r, c];
                    ApplyAllBordersAndAlignment(cell);
                    int resultCol = isActsSheet ? 18 : 19;
                    bool isGreen = (c == 1 || c == resultCol);
                    cell.Interior.Color = isGreen ? ColorPastelGreen : 0xFFFFFF;
                    Marshal.ReleaseComObject(cell);
                }
            }
        }

        private static void MergeCommonCells(Worksheet ws, int row1, int row2, bool isActsSheet)
        {
            int[] colsToMerge = isActsSheet ? new[] { 1, 2, 3, 4, 5, 6, 7, 18 } : new[] { 1, 2, 3, 4, 5, 6, 7, 19 };
            foreach (int col in colsToMerge)
            {
                string addr = $"{GetColumnLetter(col)}{row1}:{GetColumnLetter(col)}{row2}";
                Range rng = ws.Range[addr];
                rng.Merge();
                ApplyAllBordersAndAlignment(rng);
                Marshal.ReleaseComObject(rng);
            }
        }

        private static string GetColumnLetter(int col)
        {
            return col <= 26 ? ((char)('A' + col - 1)).ToString() : "A" + ((char)('A' + col - 27)).ToString();
        }

        private static void WriteActRow(Worksheet ws, int row, int num, string date,
            string performer, string serialNumber, string standNumber, int testNum,
            string vis, string impact, string fc1, string fc2, string v5fc1, string v5fc2,
            string fcTest, string extSensors, string longTest, string result)
        {
            ((Range)ws.Cells[row, 1]).Value = num;
            ((Range)ws.Cells[row, 2]).Value = date;
            ((Range)ws.Cells[row, 3]).Value = "Китай";
            ((Range)ws.Cells[row, 4]).Value = performer;
            ((Range)ws.Cells[row, 5]).Value = serialNumber;
            ((Range)ws.Cells[row, 6]).Value = "-";
            ((Range)ws.Cells[row, 7]).Value = standNumber;
            ((Range)ws.Cells[row, 8]).Value = testNum;
            ((Range)ws.Cells[row, 9]).Value = vis;
            ((Range)ws.Cells[row, 10]).Value = impact;
            ((Range)ws.Cells[row, 11]).Value = fc1;
            ((Range)ws.Cells[row, 12]).Value = fc2;
            ((Range)ws.Cells[row, 13]).Value = v5fc1;
            ((Range)ws.Cells[row, 14]).Value = v5fc2;
            ((Range)ws.Cells[row, 15]).Value = fcTest;
            ((Range)ws.Cells[row, 16]).Value = extSensors ?? "";
            ((Range)ws.Cells[row, 17]).Value = longTest;
            ((Range)ws.Cells[row, 18]).Value = result;
            ((Range)ws.Cells[row, 19]).Value = "";
        }

        private static void WriteRepairRow(Worksheet ws, int row, int num, string actNumber, string date,
            string performer, string serialNumber, string standNumber, int testNum,
            string vis, string impact, string fc1, string fc2, string v5fc1, string v5fc2,
            string fcTest, string extSensors, string longTest, string result)
        {
            ((Range)ws.Cells[row, 1]).Value = num;
            ((Range)ws.Cells[row, 2]).Value = actNumber;
            ((Range)ws.Cells[row, 3]).Value = date;
            ((Range)ws.Cells[row, 4]).Value = "Китай";
            ((Range)ws.Cells[row, 5]).Value = performer;
            ((Range)ws.Cells[row, 6]).Value = serialNumber;
            ((Range)ws.Cells[row, 7]).Value = "-";
            ((Range)ws.Cells[row, 8]).Value = standNumber;
            ((Range)ws.Cells[row, 9]).Value = testNum;
            ((Range)ws.Cells[row, 10]).Value = vis;
            ((Range)ws.Cells[row, 11]).Value = impact;
            ((Range)ws.Cells[row, 12]).Value = fc1;
            ((Range)ws.Cells[row, 13]).Value = fc2;
            ((Range)ws.Cells[row, 14]).Value = v5fc1;
            ((Range)ws.Cells[row, 15]).Value = v5fc2;
            ((Range)ws.Cells[row, 16]).Value = fcTest;
            ((Range)ws.Cells[row, 17]).Value = extSensors ?? "";
            ((Range)ws.Cells[row, 18]).Value = longTest;
            ((Range)ws.Cells[row, 19]).Value = result;
            ((Range)ws.Cells[row, 20]).Value = "";
        }

        /// <summary>Открывает файл в ассоциированном приложении.</summary>
        public static void OpenFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch { }
        }
    }
}
