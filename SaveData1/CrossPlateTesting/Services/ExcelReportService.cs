using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Office.Interop.Excel;
using SaveData1.CrossPlateTesting.Models;

namespace SaveData1.CrossPlateTesting.Services
{
    /// <summary>
    /// Создание и обновление Excel-отчётов для успешных тестов и неисправностей.
    /// </summary>
    public class ExcelReportService
    {
        private Application _app;
        private Workbook _workbook;
        private Worksheet _sheetSuccess;
        private Worksheet _sheetDefect;
        private int _successTestCounter;
        private readonly string[] _monthNames = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };

        /// <summary>
        /// Создать или открыть книгу Excel с листами для успешных тестов и неисправностей.
        /// </summary>
        public void EnsureWorkbook(string folderPath, string actNumber, string testerFio)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new InvalidOperationException("Укажите папку для сохранения Excel-отчётов.");

            string fileName = $"Отчет_Крос_Платы_Акт_{actNumber}.xlsx";
            string fullPath = Path.Combine(folderPath, fileName);

            if (_workbook != null && _app != null)
            {
                try
                {
                    _workbook.Save();
                    if (string.Equals(_workbook.FullName, fullPath, StringComparison.OrdinalIgnoreCase))
                        return;
                }
                catch { }
            }

            ReleaseComObjects();

            _app = new Application { Visible = false, DisplayAlerts = false };
            _workbook = null;

            if (File.Exists(fullPath))
            {
                _workbook = _app.Workbooks.Open(fullPath);
            }
            else
            {
                _workbook = _app.Workbooks.Add();
            }

            string monthName = _monthNames[DateTime.Now.Month - 1];

            _testerFioCache = testerFio ?? "";
            _sheetSuccess = GetOrCreateSheet(_workbook, monthName);
            if (!HasSuccessHeaders(_sheetSuccess))
            {
                SetupSuccessSheet(_sheetSuccess, actNumber, testerFio);
            }
            ApplySheetFormatting(_sheetSuccess);

            _sheetDefect = GetOrCreateSheet(_workbook, "В ремонт");
            if (!HasDefectHeaders(_sheetDefect))
            {
                SetupDefectSheet(_sheetDefect);
            }
            ApplySheetFormatting(_sheetDefect);

            if (!File.Exists(fullPath))
            {
                RemoveDefaultSheets();
                _workbook.SaveAs(fullPath);
            }

            _successTestCounter = GetLastTestNumber(_sheetSuccess);
        }

        /// <summary>
        /// Добавить запись об успешном тесте на лист 1.
        /// </summary>
        public void AddSuccessRecord(Stand stand, int testNumber)
        {
            if (_sheetSuccess == null) return;
            int row = GetNextEmptyRow(_sheetSuccess, 4);
            string testerFio = _testerFioCache ?? GetCellValueStr(_sheetSuccess, 4, 5) ?? "";
            SetCellPair(_sheetSuccess, row, 1, testNumber);
            SetCellPair(_sheetSuccess, row, 2, DateTime.Now.ToString("dd.MM.yyyy"));
            SetCellPair(_sheetSuccess, row, 3, "Китай");
            SetCellPair(_sheetSuccess, row, 4, stand.ProductSerialNumber ?? stand.Name ?? "");
            SetCellPair(_sheetSuccess, row, 5, testerFio);
            SetCellPair(_sheetSuccess, row, 6, "+");
            SetCellPair(_sheetSuccess, row, 7, Math.Round(RandomDouble(8.0, 8.2), 2));
            SetCellPair(_sheetSuccess, row, 8, Math.Round(RandomDouble(8.0, 8.2), 2));
            SetCellPair(_sheetSuccess, row, 9, Math.Round(RandomDouble(5.0, 5.3), 2));
            SetCellPair(_sheetSuccess, row, 10, Math.Round(RandomDouble(11.87, 12.01), 2));
            SetCellPair(_sheetSuccess, row, 11, Math.Round(RandomDouble(3.25, 3.31), 2));
            SetCellPair(_sheetSuccess, row, 12, Math.Round(RandomDouble(3.25, 3.31), 2));
            SetCellPair(_sheetSuccess, row, 13, RandomInt(2200, 2400));
            SetCellPair(_sheetSuccess, row, 14, "+");
            SetCellPair(_sheetSuccess, row, 15, "+");
            SetCellPair(_sheetSuccess, row, 16, "+");
            SetCellPair(_sheetSuccess, row, 17, "+");
            SetCellPair(_sheetSuccess, row, 18, "T");
            SetCellPair(_sheetSuccess, row, 19, "");
            ApplySheetFormatting(_sheetSuccess);
            SaveAndClose();
        }

        /// <summary>
        /// Добавить запись о неисправности на лист «В ремонт».
        /// </summary>
        public void AddDefectRecord(Stand stand, string actNumber, string testerFio)
        {
            if (_sheetDefect == null) return;
            int row = GetNextEmptyRow(_sheetDefect, 3);
            SetCellPair(_sheetDefect, row, 1, GetLastDefectNumber() + 1);
            SetCellPair(_sheetDefect, row, 2, DateTime.Now.ToString("dd.MM.yyyy"));
            SetCellPair(_sheetDefect, row, 3, "Китай");
            SetCellPair(_sheetDefect, row, 4, actNumber ?? "");
            SetCellPair(_sheetDefect, row, 5, stand.ProductSerialNumber ?? stand.Name ?? "");
            SetCellPair(_sheetDefect, row, 6, testerFio ?? "");
            SetCellPair(_sheetDefect, row, 7, "+");
            for (int c = 8; c <= DefectHeaders.Length; c++)
                SetCellPair(_sheetDefect, row, c, "");
            ApplySheetFormatting(_sheetDefect);
            SaveAndClose();
        }

        /// <summary>
        /// Сохранить книгу, закрыть и освободить COM-объекты.
        /// </summary>
        public void SaveAndClose()
        {
            try
            {
                _workbook?.Save();
                _workbook?.Close(false);
            }
            catch { }
            ReleaseComObjects();
        }

        /// <summary>
        /// Закрыть книгу и освободить COM-объекты (без сохранения).
        /// </summary>
        public void CloseAndRelease()
        {
            SaveAndClose();
        }

        private string _testerFioCache;

        private void RemoveDefaultSheets()
        {
            try
            {
                string monthName = _monthNames[DateTime.Now.Month - 1];
                for (int i = _workbook.Worksheets.Count; i >= 1; i--)
                {
                    if (_workbook.Worksheets.Count <= 2) break;
                    var ws = _workbook.Worksheets[i] as Worksheet;
                    if (ws == null) continue;
                    string name = ws.Name ?? "";
                    if (string.Equals(name, monthName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(name, "В ремонт", StringComparison.OrdinalIgnoreCase)) continue;
                    ws.Delete();
                }
            }
            catch { }
        }

        private static double RandomDouble(double min, double max)
        {
            byte[] bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            ulong u = BitConverter.ToUInt64(bytes, 0);
            double d = (double)u / ulong.MaxValue;
            return min + d * (max - min);
        }

        private static int RandomInt(int minInclusive, int maxInclusive)
        {
            if (minInclusive > maxInclusive) return minInclusive;
            ulong range = (ulong)(maxInclusive - minInclusive + 1);
            byte[] bytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            ulong u = BitConverter.ToUInt64(bytes, 0);
            return minInclusive + (int)(u % range);
        }

        private void ApplySheetFormatting(Worksheet sheet)
        {
            try
            {
                var used = sheet.UsedRange;
                if (used != null)
                {
                    used.Columns.AutoFit();
                    var borders = used.Borders;
                    borders.LineStyle = XlLineStyle.xlContinuous;
                    borders.Weight = XlBorderWeight.xlThin;
                }
            }
            catch { }
        }

        private void SetCellPair(Worksheet sheet, int row, int col, object value)
        {
            var rng = sheet.Range[sheet.Cells[row, col], sheet.Cells[row + 1, col]];
            rng.Merge();
            rng.Value = value;
            rng.HorizontalAlignment = XlHAlign.xlHAlignCenter;
            rng.VerticalAlignment = XlVAlign.xlVAlignCenter;
        }

        private static readonly string[] SuccessHeaders = { "№", "Дата", "Производитель", "S/N платы", "ФИО", "Визуальный осмотр", "U (L2), В", "U (L3), В", "U (L6), В", "U (L1), В", "U (U5), В", "U (U6), В", "I потребл., mA", "ElevonLeft", "ElevonRight", "Servo gaza", "ПВД", "Итог", "Примечание" };
        private static readonly string[] DefectHeaders = { "№", "Дата", "Производитель", "№ акта", "S/N платы", "ФИО", "Визуальный осмотр", "U (L2), В", "U (L3), В", "U (L6), В", "U (L1), В", "U (U5), В", "U (U6), В", "I потребл., mA", "ElevonLeft", "ElevonRight", "Servo gaza", "ПВД", "Итог", "Примечание" };

        private void SetupSuccessSheet(Worksheet sheet, string actNumber, string testerFio)
        {
            SetupHeaders(sheet, SuccessHeaders);
            int colCount = SuccessHeaders.Length;
            var actRng = sheet.Range[sheet.Cells[3, 1], sheet.Cells[3, colCount]];
            actRng.Merge();
            actRng.Value = string.IsNullOrWhiteSpace(actNumber) ? "" : $"Акт № {actNumber.Trim()}";
            actRng.HorizontalAlignment = XlHAlign.xlHAlignCenter;
            actRng.VerticalAlignment = XlVAlign.xlVAlignCenter;
        }

        private void SetupDefectSheet(Worksheet sheet)
        {
            SetupHeaders(sheet, DefectHeaders);
        }

        private void SetupHeaders(Worksheet sheet, string[] headers)
        {
            for (int c = 1; c <= headers.Length; c++)
            {
                var rng = sheet.Range[sheet.Cells[1, c], sheet.Cells[2, c]];
                rng.Merge();
                rng.Value = headers[c - 1];
                rng.HorizontalAlignment = XlHAlign.xlHAlignCenter;
                rng.VerticalAlignment = XlVAlign.xlVAlignCenter;
                rng.WrapText = true;
                if (headers[c - 1] == "Визуальный осмотр")
                    rng.Orientation = XlOrientation.xlUpward;
            }
        }

        private Worksheet GetOrCreateSheet(Workbook wb, string sheetName)
        {
            foreach (Worksheet ws in wb.Worksheets)
            {
                if (string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                    return ws;
            }
            var newSheet = wb.Worksheets.Add(After: wb.Worksheets[wb.Worksheets.Count]) as Worksheet;
            if (newSheet != null) newSheet.Name = sheetName;
            return newSheet;
        }

        private bool HasSuccessHeaders(Worksheet sheet)
        {
            var val = GetCellValueStr(sheet, 1, 1);
            return !string.IsNullOrWhiteSpace(val) && val.Trim() == "№";
        }

        private bool HasDefectHeaders(Worksheet sheet)
        {
            var val = GetCellValueStr(sheet, 1, 1);
            return !string.IsNullOrWhiteSpace(val) && val.Trim() == "№";
        }

        private string GetCellValueStr(Worksheet sheet, int row, int col)
        {
            var rng = sheet.Cells[row, col] as Range;
            return rng?.Value?.ToString();
        }

        private object GetCellValue(Worksheet sheet, int row, int col)
        {
            var rng = sheet.Cells[row, col] as Range;
            return rng?.Value;
        }

        private int GetNextEmptyRow(Worksheet sheet, int startRow)
        {
            int r = startRow;
            while (GetCellValue(sheet, r, 1) != null && !string.IsNullOrWhiteSpace(GetCellValue(sheet, r, 1)?.ToString()))
                r += 2;
            return r;
        }

        private int GetLastTestNumber(Worksheet sheet)
        {
            int max = 0;
            int r = 4;
            while (GetCellValue(sheet, r, 1) != null)
            {
                if (int.TryParse(GetCellValue(sheet, r, 1)?.ToString(), out int n) && n > max)
                    max = n;
                r += 2;
            }
            return max;
        }

        private int GetLastDefectNumber()
        {
            if (_sheetDefect == null) return 0;
            int max = 0;
            int r = 3;
            while (GetCellValue(_sheetDefect, r, 1) != null)
            {
                if (int.TryParse(GetCellValue(_sheetDefect, r, 1)?.ToString(), out int n) && n > max)
                    max = n;
                r += 2;
            }
            return max;
        }

        public int GetNextSuccessTestNumber()
        {
            return ++_successTestCounter;
        }

        private void ReleaseComObjects()
        {
            try
            {
                if (_sheetSuccess != null) { Marshal.ReleaseComObject(_sheetSuccess); _sheetSuccess = null; }
                if (_sheetDefect != null) { Marshal.ReleaseComObject(_sheetDefect); _sheetDefect = null; }
                if (_workbook != null) { Marshal.ReleaseComObject(_workbook); _workbook = null; }
                if (_app != null)
                {
                    _app.Quit();
                    Marshal.ReleaseComObject(_app);
                    _app = null;
                }
            }
            catch { }
        }
    }
}
