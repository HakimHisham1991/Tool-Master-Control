using ClosedXML.Excel;

namespace CNCToolingDatabase.Helpers;

public static class ExcelExportHelper
{
    private const double PartImageRowHeight = 100;
    private static readonly XLColor HeaderBackground = XLColor.FromArgb(204, 255, 255);

    public static void ApplyTextAlignment(IXLWorksheet worksheet, int startRow, int endRow, int endCol)
    {
        if (endRow < startRow || endCol < 1) return;
        var range = worksheet.Range(startRow, 1, endRow, endCol);
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    public static void StyleCell(IXLCell cell)
    {
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    public static void WriteHeaderRow(IXLWorksheet worksheet, int row, string[] headers)
    {
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = worksheet.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style.Fill.BackgroundColor = HeaderBackground;
            cell.Style.Font.Bold = true;
            StyleCell(cell);
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

    public static string? ResolvePartImagePath(string? partNumber)
    {
        if (string.IsNullOrWhiteSpace(partNumber)) return null;
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Data", "PART_IMAGE");
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".gif" })
        {
            var path = Path.Combine(baseDir, partNumber + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    /// <summary>Row label in A, part image in B. Row height is fixed at 100 points.</summary>
    public static void WritePartImageRow(IXLWorksheet worksheet, int row, string? partNumber, ICollection<string>? tempFiles = null)
    {
        worksheet.Cell(row, 1).Value = "Image:";
        StyleCell(worksheet.Cell(row, 1));
        worksheet.Row(row).Height = PartImageRowHeight;

        var imagePath = ResolvePartImagePath(partNumber);
        if (imagePath == null) return;

        var preparedPath = PdfImageHelper.PrepareImagePath(imagePath, tempFiles);
        if (preparedPath == null) return;

        var picture = worksheet.AddPicture(preparedPath);
        const int margin = 4;
        const double maxHeight = PartImageRowHeight - margin * 2;
        picture.MoveTo(worksheet.Cell(row, 2), margin, margin);

        if (picture.OriginalHeight > 0)
        {
            var scale = maxHeight / picture.OriginalHeight;
            picture.Height = (int)Math.Round(maxHeight);
            picture.Width = (int)Math.Round(picture.OriginalWidth * scale);
        }
    }

    public static void EnsurePartImageRowHeight(IXLWorksheet worksheet, int row)
    {
        worksheet.Row(row).Height = PartImageRowHeight;
    }
}
