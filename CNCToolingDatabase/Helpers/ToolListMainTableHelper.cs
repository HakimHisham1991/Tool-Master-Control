using CNCToolingDatabase.Models.ViewModels;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;

namespace CNCToolingDatabase.Helpers;

/// <summary>
/// Shared 12-column tool list table used by legacy PDF export and layout renderer.
/// </summary>
internal static class ToolListMainTableHelper
{
    private static readonly Unit OriginalMainTableWidth =
        Unit.FromPoint(45 + 40 + 40 + 40 + 45 + 55) + Unit.FromCentimeter(2.8 + 2.8 + 1.7 + 1.7 + 2.8 + 2.2);

    private static readonly string[] ToolHeaders =
    {
        "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier", "Tool Holder",
        "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)", "Tool Corner Radius",
        "Arbor Description (or equivalent specs)", "Tool Path Time in Minutes", "Remarks"
    };

    public static Table CreateMainTable(DocumentObject container, Unit contentWidth, double borderWidthPt)
    {
        var table = container switch
        {
            Section section => section.AddTable(),
            TextFrame frame => frame.AddTable(),
            _ => throw new InvalidOperationException("Unsupported container for tool table")
        };

        table.Borders.Width = Unit.FromPoint(borderWidthPt);
        table.Borders.Color = Colors.Black;

        var scale = contentWidth.Point / OriginalMainTableWidth.Point;

        table.AddColumn(Unit.FromPoint(45 * scale));
        table.AddColumn(Unit.FromCentimeter(2.8 * scale));
        table.AddColumn(Unit.FromCentimeter(2.8 * scale));
        table.AddColumn(Unit.FromCentimeter(1.7 * scale));
        table.AddColumn(Unit.FromCentimeter(1.7 * scale));
        table.AddColumn(Unit.FromPoint(40 * scale));
        table.AddColumn(Unit.FromPoint(40 * scale));
        table.AddColumn(Unit.FromPoint(40 * scale));
        table.AddColumn(Unit.FromPoint(45 * scale));
        table.AddColumn(Unit.FromCentimeter(2.8 * scale));
        table.AddColumn(Unit.FromPoint(55 * scale));
        table.AddColumn(Unit.FromCentimeter(2.2 * scale));

        return table;
    }

    public static void AddToolRows(
        Table table,
        IReadOnlyList<ToolListDetailRow> details,
        string fontName,
        Color headerFill)
    {
        var headerRow = table.AddRow();
        headerRow.VerticalAlignment = VerticalAlignment.Center;
        for (int i = 0; i < ToolHeaders.Length; i++)
            StyleHeaderCell(headerRow.Cells[i], ToolHeaders[i], fontName, headerFill);

        foreach (var detail in details)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            var values = new[]
            {
                detail.ToolNumber ?? "",
                detail.ToolDescription ?? "",
                detail.ConsumableCode ?? "",
                detail.Supplier ?? "",
                detail.HolderExtensionCode ?? "",
                (detail.Diameter ?? 0).ToString("0.##"),
                (detail.FluteLength ?? 0).ToString("0.##"),
                (detail.ProtrusionLength ?? 0).ToString("0.##"),
                (detail.CornerRadius ?? 0).ToString("0.##"),
                detail.ArborCode ?? "",
                (detail.ToolPathTimeMinutes ?? 0).ToString("0.##"),
                detail.Remarks ?? ""
            };

            for (int i = 0; i < values.Length; i++)
                StyleDataCell(row.Cells[i], values[i], fontName);
        }
    }

    private static void StyleHeaderCell(Cell cell, string text, string fontName, Color headerFill)
    {
        cell.Shading.Color = headerFill;
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Name = fontName;
        paragraph.Format.Font.Size = Unit.FromPoint(6);
        paragraph.Format.Font.Bold = true;
    }

    private static void StyleDataCell(Cell cell, string text, string fontName)
    {
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Name = fontName;
        paragraph.Format.Font.Size = Unit.FromPoint(6);
    }
}
