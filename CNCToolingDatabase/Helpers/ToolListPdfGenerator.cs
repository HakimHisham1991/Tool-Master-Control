using CNCToolingDatabase.Models.ViewModels;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace CNCToolingDatabase.Helpers;

public static class ToolListPdfGenerator
{
    private const string FontName = "Arial";
    private static readonly Color HeaderFill = new(204, 255, 255);
    private static readonly Unit BorderWidth = Unit.FromPoint(0.5);
    private static readonly Unit Margin = Unit.FromCentimeter(1.5);
    private static readonly Unit ImageRowHeight = Unit.FromPoint(45);

    // A4 landscape page width minus left/right margins (29.7 - 2 * 1.5 cm).
    private static readonly Unit ContentWidth = Unit.FromCentimeter(29.7 - 2 * 1.5);

    // Original sum of the 12 column widths in CreateMainColumnTable. The columns are
    // scaled by ContentWidth / OriginalMainTableWidth so the table fills the page width.
    private static readonly Unit OriginalMainTableWidth =
        Unit.FromPoint(45 + 40 + 40 + 40 + 45 + 55) + Unit.FromCentimeter(2.8 + 2.8 + 1.7 + 1.7 + 2.8 + 2.2);
    private static readonly double WidthScale = ContentWidth.Point / OriginalMainTableWidth.Point;

    // Total width of the main tool table after scaling = page content width.
    // Keeping this in one place ensures the specs table above stays aligned with the
    // rightmost border of the main tool table when the column layout changes.
    private static readonly Unit MainTableWidth = ContentWidth;
    private static readonly Unit SpecsLabelWidth = Unit.FromCentimeter(4);

    public static byte[] Generate(
        ToolListEditorViewModel viewModel,
        IReadOnlyList<ToolListDetailRow> details,
        byte[]? camProgrammerStamp,
        byte[]? approvedByStamp,
        byte[]? toolRegisterStamp,
        string? toolRegisterByName,
        string? logoPath,
        string? partImagePath,
        string? toolSpecsPath,
        Models.PdfLayout.PdfLayoutDocument? layout = null)
    {
        if (layout != null)
        {
            return PdfLayoutRenderer.Generate(
                layout, viewModel, details,
                camProgrammerStamp, approvedByStamp, toolRegisterStamp,
                toolRegisterByName, logoPath, partImagePath, toolSpecsPath);
        }

        var tempFiles = new List<string>();
        try
        {
            PdfFontBootstrap.EnsureInitialized();

            var document = new Document();
            document.Info.Title = viewModel.ToolListName ?? "Master Tooling List";

            var section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.LeftMargin = Margin;
            section.PageSetup.RightMargin = Margin;
            section.PageSetup.TopMargin = Margin;
            section.PageSetup.BottomMargin = Margin;

            AddHeader(section, logoPath);
            AddInfoTable(section, viewModel);
            AddSpacer(section, Unit.FromPoint(6));
            AddSpecsTable(section, viewModel);
            AddImageRow(section, partImagePath, toolSpecsPath);
            AddToolTable(section, details);
            AddStampSection(section, camProgrammerStamp, approvedByStamp, toolRegisterStamp, toolRegisterByName, viewModel, tempFiles);
            AddFooter(section);

            using var stream = new MemoryStream();
            var renderer = new PdfDocumentRenderer { Document = document };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(stream, closeStream: false);
            return stream.ToArray();
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                try { File.Delete(tempFile); } catch { /* best effort cleanup */ }
            }
        }
    }

    private static void AddHeader(Section section, string? logoPath)
    {
        var table = section.AddTable();
        table.Borders.Visible = false;
        table.AddColumn(Unit.FromCentimeter(2.8));
        table.AddColumn(Unit.FromCentimeter(23.4));

        var row = table.AddRow();
        row.VerticalAlignment = VerticalAlignment.Center;

        var logoCell = row.Cells[0];
        logoCell.VerticalAlignment = VerticalAlignment.Center;
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            var paragraph = logoCell.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Left;
            var image = paragraph.AddImage(logoPath);
            image.Width = Unit.FromCentimeter(2.5);
            image.LockAspectRatio = true;
        }

        var titleCell = row.Cells[1];
        titleCell.VerticalAlignment = VerticalAlignment.Center;
        var title = titleCell.AddParagraph("Master Tooling List");
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.Font.Name = FontName;
        title.Format.Font.Size = 22;
        title.Format.Font.Bold = true;
    }

    private static void AddInfoTable(Section section, ToolListEditorViewModel viewModel)
    {
        var table = section.AddTable();
        table.Borders.Visible = false;
        table.AddColumn(Unit.FromCentimeter(2.8));
        table.AddColumn(Unit.FromCentimeter(5.6));
        table.AddColumn(Unit.FromCentimeter(2.8));
        table.AddColumn(Unit.FromCentimeter(5.6));
        table.AddColumn(Unit.FromCentimeter(2.8));
        table.AddColumn(Unit.FromCentimeter(5.8));

        AddInfoRow(table,
            "Tool List No.", viewModel.ToolListName ?? "",
            "Part Description:", viewModel.PartDescription ?? "",
            "Project Code", viewModel.ProjectCode ?? "");
        AddInfoRow(table,
            "Unit:", "MM",
            "Work Centre:", viewModel.MachineWorkcenter ?? "",
            "Machine Model:", viewModel.MachineModel ?? "");
    }

    private static void AddInfoRow(Table table, params string[] values)
    {
        var row = table.AddRow();
        for (int i = 0; i < values.Length && i < row.Cells.Count; i++)
        {
            var cell = row.Cells[i];
            cell.VerticalAlignment = VerticalAlignment.Center;
            var paragraph = cell.AddParagraph(values[i]);
            paragraph.Format.Font.Name = FontName;
            paragraph.Format.Font.Size = 6;
        }
    }

    private static void AddSpacer(Section section, Unit height)
    {
        var paragraph = section.AddParagraph();
        paragraph.Format.SpaceAfter = height;
    }

    private static void AddSpecsTable(Section section, ToolListEditorViewModel viewModel)
    {
        var table = section.AddTable();
        table.Borders.Width = BorderWidth;
        table.Borders.Color = Colors.Black;

        table.AddColumn(SpecsLabelWidth);
        table.AddColumn(MainTableWidth - SpecsLabelWidth);

        var rows = new (string Label, string Value)[]
        {
            ("Tool List:", viewModel.ToolListName ?? ""),
            ("Part Number:", viewModel.PartNumber ?? ""),
            ("Part Description:", viewModel.PartDescription ?? ""),
            ("Operation:", viewModel.Operation ?? ""),
            ("Revision:", viewModel.Revision ?? ""),
            ("Project Code:", viewModel.ProjectCode ?? ""),
            ("Machine:", viewModel.MachineName ?? ""),
            ("Workcenter:", viewModel.MachineWorkcenter ?? ""),
            ("Machine Model:", viewModel.MachineModel ?? ""),
        };

        foreach (var (label, value) in rows)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            StyleSpecsLabelCell(row.Cells[0], label);
            StyleSpecsValueCell(row.Cells[1], value);
        }
    }

    private static void StyleSpecsLabelCell(Cell cell, string text)
    {
        cell.Shading.Color = HeaderFill;
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Left;
        paragraph.Format.Font.Name = FontName;
        paragraph.Format.Font.Size = 8;
        paragraph.Format.Font.Bold = true;
    }

    private static void StyleSpecsValueCell(Cell cell, string text)
    {
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Left;
        paragraph.Format.Font.Name = FontName;
        paragraph.Format.Font.Size = 8;
    }

    private static void AddImageRow(Section section, string? partImagePath, string? toolSpecsPath)
    {
        var table = CreateMainColumnTable(section);

        var row = table.AddRow();
        row.Height = ImageRowHeight;
        row.HeightRule = RowHeightRule.AtLeast;
        row.VerticalAlignment = VerticalAlignment.Center;

        row.Cells[0].MergeRight = 10;
        StyleCell(row.Cells[0], ParagraphAlignment.Center);
        AddImageToCell(row.Cells[0], partImagePath, ImageRowHeight);

        StyleCell(row.Cells[11], ParagraphAlignment.Center);
        AddImageToCell(row.Cells[11], toolSpecsPath, ImageRowHeight);
    }

    private static void AddToolTable(Section section, IReadOnlyList<ToolListDetailRow> details)
    {
        var table = CreateMainColumnTable(section);

        var headers = new[]
        {
            "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier", "Tool Holder",
            "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)", "Tool Corner Radius",
            "Arbor Description (or equivalent specs)", "Tool Path Time in Minutes", "Remarks"
        };

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true;
        headerRow.VerticalAlignment = VerticalAlignment.Center;
        for (int i = 0; i < headers.Length; i++)
        {
            StyleHeaderCell(headerRow.Cells[i], headers[i]);
        }

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
            {
                StyleDataCell(row.Cells[i], values[i]);
            }
        }
    }

    private static Table CreateMainColumnTable(Section section)
    {
        var table = section.AddTable();
        table.Borders.Width = BorderWidth;
        table.Borders.Color = Colors.Black;

        table.AddColumn(Unit.FromPoint(45 * WidthScale));
        table.AddColumn(Unit.FromCentimeter(2.8 * WidthScale));
        table.AddColumn(Unit.FromCentimeter(2.8 * WidthScale));
        table.AddColumn(Unit.FromCentimeter(1.7 * WidthScale));
        table.AddColumn(Unit.FromCentimeter(1.7 * WidthScale));
        table.AddColumn(Unit.FromPoint(40 * WidthScale));
        table.AddColumn(Unit.FromPoint(40 * WidthScale));
        table.AddColumn(Unit.FromPoint(40 * WidthScale));
        table.AddColumn(Unit.FromPoint(45 * WidthScale));
        table.AddColumn(Unit.FromCentimeter(2.8 * WidthScale));
        table.AddColumn(Unit.FromPoint(55 * WidthScale));
        table.AddColumn(Unit.FromCentimeter(2.2 * WidthScale));
        return table;
    }

    private static void StyleHeaderCell(Cell cell, string text)
    {
        cell.Shading.Color = HeaderFill;
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Name = FontName;
        paragraph.Format.Font.Size = 6;
        paragraph.Format.Font.Bold = true;
    }

    private static void StyleDataCell(Cell cell, string text)
    {
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph(text);
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Name = FontName;
        paragraph.Format.Font.Size = 6;
    }

    private static void StyleCell(Cell cell, ParagraphAlignment alignment)
    {
        cell.VerticalAlignment = VerticalAlignment.Center;
        cell.Format.Alignment = alignment;
    }

    private static void AddImageToCell(Cell cell, string? path, Unit maxHeight)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        var paragraph = cell.AddParagraph();
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        var image = paragraph.AddImage(path);
        image.Height = maxHeight;
        image.LockAspectRatio = true;
    }

    private static void AddStampSection(
        Section section,
        byte[]? camProgrammerStamp,
        byte[]? approvedByStamp,
        byte[]? toolRegisterStamp,
        string? toolRegisterByName,
        ToolListEditorViewModel viewModel,
        List<string> tempFiles)
    {
        AddSpacer(section, Unit.FromCentimeter(1));

        var table = section.AddTable();
        table.Borders.Visible = false;
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));
        table.AddColumn(Unit.FromCentimeter(8.9));

        var row = table.AddRow();
        AddStampCell(row.Cells[0], ParagraphAlignment.Left, "CAM Programmer:", viewModel.CamProgrammer ?? "", camProgrammerStamp, viewModel.ApprovedDate, tempFiles);
        AddStampCell(row.Cells[1], ParagraphAlignment.Center, "Approved by:", viewModel.ApprovedBy ?? "", approvedByStamp, viewModel.CamLeaderApprovedDate, tempFiles);
        AddStampCell(row.Cells[2], ParagraphAlignment.Right, "Tool Register By:", toolRegisterByName ?? "", toolRegisterStamp, viewModel.ToolRegisterByDate, tempFiles);
    }

    private static void AddStampCell(Cell cell, ParagraphAlignment alignment, string label, string name, byte[]? stampBytes, DateTime? date, List<string> tempFiles)
    {
        cell.VerticalAlignment = VerticalAlignment.Top;
        var labelParagraph = cell.AddParagraph();
        labelParagraph.Format.Alignment = alignment;
        labelParagraph.Format.Font.Name = FontName;
        labelParagraph.Format.Font.Size = 9;
        labelParagraph.Format.SpaceAfter = Unit.FromPoint(4);
        var labelText = labelParagraph.AddFormattedText(label);
        labelText.Bold = true;
        if (!string.IsNullOrWhiteSpace(name))
        {
            labelParagraph.AddText($" {name}");
        }

        var stampPath = WriteTempImage(stampBytes, tempFiles);
        if (stampPath != null)
        {
            var stampParagraph = cell.AddParagraph();
            stampParagraph.Format.Alignment = alignment;
            var image = stampParagraph.AddImage(stampPath);
            image.Width = Unit.FromPoint(55);
            image.Height = Unit.FromPoint(55);
            image.LockAspectRatio = true;
        }
        else
        {
            var placeholder = cell.AddParagraph();
            placeholder.Format.Alignment = alignment;
            placeholder.Format.SpaceBefore = Unit.FromPoint(55);
        }

        if (date.HasValue)
        {
            var dateParagraph = cell.AddParagraph(date.Value.ToString("dd/MM/yyyy"));
            dateParagraph.Format.Alignment = alignment;
            dateParagraph.Format.Font.Name = FontName;
            dateParagraph.Format.Font.Size = 8;
            dateParagraph.Format.SpaceBefore = Unit.FromPoint(4);
        }
    }

    private static string? WriteTempImage(byte[]? imageBytes, List<string> tempFiles)
    {
        if (imageBytes is not { Length: > 0 }) return null;
        var path = Path.Combine(Path.GetTempPath(), $"tooling-stamp-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, imageBytes);
        tempFiles.Add(path);
        return path;
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Name = FontName;
        footer.Format.Font.Size = 9;
        footer.AddText("Page ");
        footer.AddPageField();
    }
}
