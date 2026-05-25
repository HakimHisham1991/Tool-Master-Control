using System.Globalization;
using ClosedXML.Excel;

namespace CNCToolingDatabase.Helpers;

public static class ExcelHelper
{
    public static int GetUsedColumnCount(IXLWorksheet sheet) =>
        sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

    public static int GetUsedRowCount(IXLWorksheet sheet) =>
        sheet.LastRowUsed()?.RowNumber() ?? 0;

    public static int GetColumn(IXLWorksheet sheet, int totalCols, params string[] headerNames)
    {
        for (int c = 1; c <= totalCols; c++)
        {
            var v = GetString(sheet, 1, c);
            if (string.IsNullOrEmpty(v)) continue;
            foreach (var h in headerNames)
            {
                if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase))
                    return c;
            }
        }
        return -1;
    }

    public static int GetColumn(IXLWorksheet sheet, int totalCols, string headerName) =>
        GetColumn(sheet, totalCols, new[] { headerName });

    public static string GetString(IXLWorksheet sheet, int row, int col)
    {
        if (col < 1) return "";
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return "";
        if (cell.DataType == XLDataType.Number)
        {
            var d = cell.GetDouble();
            if (d == Math.Floor(d) && Math.Abs(d) < 1e15)
                return ((long)d).ToString(CultureInfo.InvariantCulture);
            return d.ToString(CultureInfo.InvariantCulture);
        }
        return cell.GetString().Trim();
    }

    public static string GetPasswordString(IXLWorksheet sheet, int row, int col)
    {
        if (col < 1) return "";
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return "";
        if (cell.DataType == XLDataType.Number)
        {
            var d = cell.GetDouble();
            return d == Math.Floor(d)
                ? ((long)d).ToString(CultureInfo.InvariantCulture)
                : d.ToString(CultureInfo.InvariantCulture);
        }
        return cell.GetString().Trim();
    }

    public static decimal ParseDecimal(IXLWorksheet sheet, int row, int col)
    {
        if (col < 1) return 0;
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return 0;
        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();
        if (decimal.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            return dec;
        return 0;
    }

    public static bool ParseStatusActive(IXLWorksheet sheet, int row, int col)
    {
        var val = GetString(sheet, row, col);
        if (string.IsNullOrWhiteSpace(val)) return true;
        if (string.Equals(val, "INACTIVE", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(val, "NO", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(val, "0", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
}
