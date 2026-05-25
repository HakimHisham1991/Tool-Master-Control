using ClosedXML.Excel;

namespace CNCToolingDatabase.Helpers;

public static class ExcelExportHelper
{
    private static readonly XLColor HeaderBackground = XLColor.FromArgb(204, 255, 255);

    public static void WriteHeaderRow(IXLWorksheet worksheet, int row, string[] headers)
    {
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = worksheet.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style.Fill.BackgroundColor = HeaderBackground;
            cell.Style.Font.Bold = true;
        }
    }

    public static void ApplyTableBorders(IXLWorksheet worksheet, int startRow, int endRow, int colCount)
    {
        if (endRow < startRow || colCount < 1) return;
        var range = worksheet.Range(startRow, 1, endRow, colCount);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    public static void AutoFitColumns(IXLWorksheet worksheet)
    {
        worksheet.Columns().AdjustToContents();
    }

    public static byte[] SaveToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
