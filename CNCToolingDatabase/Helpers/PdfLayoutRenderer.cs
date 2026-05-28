using CNCToolingDatabase.Models.PdfLayout;
using CNCToolingDatabase.Models.ViewModels;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace CNCToolingDatabase.Helpers;

public static class PdfLayoutRenderer
{
    public static byte[] Generate(
        PdfLayoutDocument layout,
        ToolListEditorViewModel viewModel,
        IReadOnlyList<ToolListDetailRow> details,
        byte[]? camProgrammerStamp,
        byte[]? approvedByStamp,
        byte[]? toolRegisterStamp,
        string? toolRegisterByName,
        string? logoPath,
        string? partImagePath,
        string? toolSpecsPath)
    {
        var tempFiles = new List<string>();
        try
        {
            PdfFontBootstrap.EnsureInitialized();

            var document = new Document();
            document.Info.Title = viewModel.ToolListName ?? "Master Tooling List";

            var section = document.AddSection();
            ApplyPageSetup(section, layout.PageSetup);

            var margin = Unit.FromCentimeter(layout.PageSetup.MarginCm);
            var contentWidth = GetContentWidth(layout.PageSetup);
            var ctx = new RenderContext(
                layout,
                viewModel,
                details,
                camProgrammerStamp,
                approvedByStamp,
                toolRegisterStamp,
                toolRegisterByName,
                logoPath,
                partImagePath,
                toolSpecsPath,
                tempFiles,
                contentWidth);

            var flowElements = layout.Elements
                .Where(e => e.Visible && e.LayoutMode == "flow")
                .OrderBy(e => e.Y)
                .ThenBy(e => e.ZIndex)
                .ToList();

            var headerIds = new HashSet<string> { "header-logo", "header-title" };
            RenderHeaderSection(section, layout, ctx, headerIds);

            foreach (var element in flowElements)
            {
                if (element.Id == "footer" || element.DataBinding == "pageNumber")
                    continue;
                if (ShouldSkipElement(element))
                    continue;
                RenderFlowElement(section, element, ctx);
            }

            var footer = layout.Elements.FirstOrDefault(e =>
                e.Visible && (e.Id == "footer" || e.DataBinding == "pageNumber"));
            if (footer != null)
                RenderFlowText(section, footer, ctx);

            var absoluteElements = layout.Elements
                .Where(e => e.Visible && e.LayoutMode != "flow" && !headerIds.Contains(e.Id))
                .OrderBy(e => e.ZIndex)
                .ToList();

            foreach (var element in absoluteElements)
            {
                if (ShouldSkipElement(element))
                    continue;
                RenderAbsoluteElement(section, element, ctx, margin);
            }

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
                try { File.Delete(tempFile); } catch { /* best effort */ }
            }
        }
    }

    private static void ApplyPageSetup(Section section, PdfPageSetup setup)
    {
        section.PageSetup.PageFormat = setup.Format.Equals("A4", StringComparison.OrdinalIgnoreCase)
            ? PageFormat.A4
            : PageFormat.A4;
        section.PageSetup.Orientation = setup.Orientation.Equals("Portrait", StringComparison.OrdinalIgnoreCase)
            ? Orientation.Portrait
            : Orientation.Landscape;
        var margin = Unit.FromCentimeter(setup.MarginCm);
        section.PageSetup.LeftMargin = margin;
        section.PageSetup.RightMargin = margin;
        section.PageSetup.TopMargin = margin;
        section.PageSetup.BottomMargin = margin;
    }

    private static Unit GetContentWidth(PdfPageSetup setup)
    {
        var pageWidth = setup.Orientation.Equals("Portrait", StringComparison.OrdinalIgnoreCase)
            ? 21.0
            : 29.7;
        return Unit.FromCentimeter(pageWidth - 2 * setup.MarginCm);
    }

    private static bool ShouldSkipElement(PdfLayoutElement element) =>
        element.Id == "info-table" ||
        string.Equals(element.TableKind, "info", StringComparison.OrdinalIgnoreCase);

    private static void RenderHeaderSection(Section section, PdfLayoutDocument layout, RenderContext ctx, HashSet<string> headerIds)
    {
        var logoEl = layout.Elements.FirstOrDefault(e => e.Id == "header-logo" && e.Visible);
        var titleEl = layout.Elements.FirstOrDefault(e => e.Id == "header-title" && e.Visible);
        if (logoEl == null && titleEl == null) return;

        var table = section.AddTable();
        table.Borders.Visible = false;
        table.AddColumn(Unit.FromCentimeter(logoEl?.Width ?? 2.8));
        table.AddColumn(Unit.FromCentimeter(titleEl?.Width ?? ctx.ContentWidth.Centimeter - (logoEl?.Width ?? 2.8)));

        var row = table.AddRow();
        row.VerticalAlignment = VerticalAlignment.Center;

        var logoCell = row.Cells[0];
        logoCell.VerticalAlignment = VerticalAlignment.Center;
        var logoPath = ResolveImagePath(logoEl?.ImageSource ?? "logo", ctx);
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            var logoParagraph = logoCell.AddParagraph();
            logoParagraph.Format.Alignment = ParagraphAlignment.Left;
            var image = logoParagraph.AddImage(logoPath);
            image.Width = Unit.FromCentimeter(logoEl?.Width > 0 ? logoEl.Width - 0.3 : 2.5);
            image.LockAspectRatio = true;
        }

        var titleCell = row.Cells[1];
        titleCell.VerticalAlignment = VerticalAlignment.Center;
        var titleParagraph = titleCell.AddParagraph(titleEl?.Text ?? "Master Tooling List");
        titleParagraph.Format.Alignment = ToAlignment(titleEl?.Align ?? "center");
        titleParagraph.Format.Font.Name = ctx.Layout.Styles.FontName;
        titleParagraph.Format.Font.Size = titleEl?.FontSize ?? 22;
        titleParagraph.Format.Font.Bold = titleEl?.Bold ?? true;
    }

    private static void RenderFlowElement(Section section, PdfLayoutElement element, RenderContext ctx)
    {
        var gap = GetGapBefore(element);
        if (gap > 0)
        {
            var spacer = section.AddParagraph();
            spacer.Format.SpaceAfter = Unit.FromCentimeter(gap);
        }

        switch (element.Type)
        {
            case "table":
                RenderTable(section, element, ctx);
                break;
            case "text":
                RenderFlowText(section, element, ctx);
                break;
            case "image":
                RenderFlowImage(section, element, ctx);
                break;
            case "box":
                RenderFlowBox(section, element, ctx);
                break;
        }
    }

    private static double GetGapBefore(PdfLayoutElement element)
    {
        if (element.GapBeforeCm > 0) return element.GapBeforeCm;
        return element.Id switch
        {
            "specs-table" => 0.21,
            _ => 0
        };
    }

    private static void RenderAbsoluteElement(Section section, PdfLayoutElement element, RenderContext ctx, Unit margin)
    {
        var frame = section.AddTextFrame();
        frame.Left = margin + Unit.FromCentimeter(element.X);
        frame.Top = margin + Unit.FromCentimeter(element.Y);
        frame.Width = Unit.FromCentimeter(element.Width);
        frame.Height = Unit.FromCentimeter(element.Height);
        frame.RelativeHorizontal = RelativeHorizontal.Page;
        frame.RelativeVertical = RelativeVertical.Page;

        switch (element.Type)
        {
            case "text":
                RenderTextInFrame(frame, element, ctx);
                break;
            case "image":
                RenderImageInFrame(frame, element, ctx);
                break;
            case "box":
                RenderBoxInFrame(frame, element, ctx);
                break;
            case "table":
                RenderTableInFrame(frame, element, ctx);
                break;
            case "cell":
                RenderCellInFrame(frame, element, ctx);
                break;
        }
    }

    private static void RenderFlowText(Section section, PdfLayoutElement element, RenderContext ctx)
    {
        if (element.DataBinding == "pageNumber")
        {
            var footer = section.Footers.Primary.AddParagraph();
            footer.Format.Alignment = ToAlignment(element.Align);
            footer.Format.Font.Name = ctx.Layout.Styles.FontName;
            footer.Format.Font.Size = element.FontSize;
            footer.AddText("Page ");
            footer.AddPageField();
            return;
        }

        var paragraph = section.AddParagraph(ResolveText(element, ctx));
        ApplyTextFormat(paragraph, element, ctx);
    }

    private static void RenderFlowImage(Section section, PdfLayoutElement element, RenderContext ctx)
    {
        var path = ResolveImagePath(element.ImageSource, ctx);
        if (path == null) return;
        var paragraph = section.AddParagraph();
        paragraph.Format.Alignment = ToAlignment(element.Align);
        var image = paragraph.AddImage(path);
        if (element.Height > 0) image.Height = Unit.FromCentimeter(element.Height);
        if (element.Width > 0) image.Width = Unit.FromCentimeter(element.Width);
        image.LockAspectRatio = element.LockAspectRatio;
    }

    private static void RenderFlowBox(Section section, PdfLayoutElement element, RenderContext ctx)
    {
        var table = section.AddTable();
        table.Borders.Width = element.ShowBorder ? Unit.FromPoint(ctx.Layout.Styles.BorderWidthPt) : 0;
        table.AddColumn(Unit.FromCentimeter(element.Width));
        var row = table.AddRow();
        row.Height = Unit.FromCentimeter(element.Height);
        var cell = row.Cells[0];
        ApplyCellBackground(cell, element.BackgroundColor ?? ctx.Layout.Styles.HeaderFill);
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            var p = cell.AddParagraph(element.Text);
            ApplyTextFormat(p, element, ctx);
        }
    }

    private static void RenderTextInFrame(TextFrame frame, PdfLayoutElement element, RenderContext ctx)
    {
        var paragraph = frame.AddParagraph(ResolveText(element, ctx));
        ApplyTextFormat(paragraph, element, ctx);
    }

    private static void RenderImageInFrame(TextFrame frame, PdfLayoutElement element, RenderContext ctx)
    {
        var path = ResolveImagePath(element.ImageSource, ctx);
        if (path == null) return;
        var paragraph = frame.AddParagraph();
        paragraph.Format.Alignment = ToAlignment(element.Align);
        var image = paragraph.AddImage(path);
        if (element.Height > 0) image.Height = Unit.FromCentimeter(element.Height);
        else if (element.Width > 0) image.Width = Unit.FromCentimeter(element.Width);
        image.LockAspectRatio = element.LockAspectRatio;
    }

    private static void RenderBoxInFrame(TextFrame frame, PdfLayoutElement element, RenderContext ctx)
    {
        var table = frame.AddTable();
        table.Borders.Width = element.ShowBorder ? Unit.FromPoint(ctx.Layout.Styles.BorderWidthPt) : 0;
        table.AddColumn(Unit.FromCentimeter(element.Width));
        var row = table.AddRow();
        var cell = row.Cells[0];
        ApplyCellBackground(cell, element.BackgroundColor);
        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            var p = cell.AddParagraph(element.Text);
            ApplyTextFormat(p, element, ctx);
        }
    }

    private static void RenderCellInFrame(TextFrame frame, PdfLayoutElement element, RenderContext ctx)
    {
        var table = frame.AddTable();
        table.Borders.Width = element.ShowBorder ? Unit.FromPoint(ctx.Layout.Styles.BorderWidthPt) : Unit.FromPoint(ctx.Layout.Styles.BorderWidthPt);
        table.AddColumn(Unit.FromCentimeter(element.Width));
        var row = table.AddRow();
        var cell = row.Cells[0];
        ApplyCellBackground(cell, element.BackgroundColor);
        var p = cell.AddParagraph(ResolveText(element, ctx));
        ApplyTextFormat(p, element, ctx);
    }

    private static void RenderTableInFrame(TextFrame frame, PdfLayoutElement element, RenderContext ctx)
    {
        RenderTableCore(frame, element, ctx);
    }

    private static void RenderTable(Section section, PdfLayoutElement element, RenderContext ctx)
    {
        RenderTableCore(section, element, ctx);
    }

    private static void RenderTableCore(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        switch (element.TableKind)
        {
            case "info":
                RenderInfoTable(container, element, ctx);
                break;
            case "specs":
                RenderSpecsTable(container, element, ctx);
                break;
            case "imageRow":
                RenderImageRowTable(container, element, ctx);
                break;
            case "tool":
                RenderToolTable(container, element, ctx);
                break;
            case "stamps":
                RenderStampTable(container, element, ctx);
                break;
            default:
                RenderGenericTable(container, element, ctx);
                break;
        }
    }

    private static void RenderInfoTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        var table = AddTable(container);
        table.Borders.Visible = false;
        var colWidth = element.Width / 6;
        for (int i = 0; i < 6; i++)
            table.AddColumn(Unit.FromCentimeter(colWidth));

        foreach (var rowDef in element.Rows ?? new List<PdfTableRow>())
        {
            var row = table.AddRow();
            for (int i = 0; i < rowDef.Cells.Count && i < 6; i++)
            {
                var cellDef = rowDef.Cells[i];
                var cell = row.Cells[i];
                cell.VerticalAlignment = VerticalAlignment.Center;
                var p = cell.AddParagraph(ResolveCellText(cellDef, ctx));
                p.Format.Font.Name = ctx.Layout.Styles.FontName;
                p.Format.Font.Size = cellDef.FontSize > 0 ? cellDef.FontSize : 6;
                p.Format.Font.Bold = cellDef.Bold;
            }
        }
    }

    private static void RenderSpecsTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        var table = AddTable(container);
        ApplyTableBorder(table, element, ctx);
        var labelWidth = Unit.FromCentimeter(4);
        table.AddColumn(labelWidth);
        table.AddColumn(ctx.ContentWidth - labelWidth);

        foreach (var rowDef in element.Rows ?? new List<PdfTableRow>())
        {
            if (rowDef.Cells.Count < 2) continue;
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            StyleSpecsCell(row.Cells[0], rowDef.Cells[0], ctx, true);
            StyleSpecsCell(row.Cells[1], rowDef.Cells[1], ctx, false);
        }
    }

    private static void StyleSpecsCell(Cell cell, PdfTableCell cellDef, RenderContext ctx, bool isLabel)
    {
        if (isLabel || cellDef.IsLabel)
            ApplyCellBackground(cell, cellDef.BackgroundColor ?? ctx.Layout.Styles.HeaderFill);
        cell.VerticalAlignment = VerticalAlignment.Center;
        var p = cell.AddParagraph(ResolveCellText(cellDef, ctx));
        p.Format.Alignment = ToAlignment(cellDef.Align);
        p.Format.Font.Name = ctx.Layout.Styles.FontName;
        p.Format.Font.Size = cellDef.FontSize > 0 ? cellDef.FontSize : 8;
        p.Format.Font.Bold = cellDef.Bold || isLabel;
    }

    private static void RenderImageRowTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        var table = CreateScaledColumnTable(container, element, ctx);
        var row = table.AddRow();
        row.Height = Unit.FromPoint(45);
        row.HeightRule = RowHeightRule.AtLeast;
        row.VerticalAlignment = VerticalAlignment.Center;

        row.Cells[0].MergeRight = 10;
        AddImageToCell(row.Cells[0], ctx.PartImagePath, row.Height);

        if (row.Cells.Count > 11)
            AddImageToCell(row.Cells[11], ctx.ToolSpecsPath, row.Height);
    }

    private static void RenderToolTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        var table = CreateScaledColumnTable(container, element, ctx);
        var columns = GetToolTableColumns(element);

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true;
        headerRow.VerticalAlignment = VerticalAlignment.Center;
        for (int i = 0; i < columns.Count && i < headerRow.Cells.Count; i++)
        {
            var col = columns[i];
            var cell = headerRow.Cells[i];
            ApplyCellBackground(cell, ctx.Layout.Styles.HeaderFill);
            cell.VerticalAlignment = VerticalAlignment.Center;
            var p = cell.AddParagraph(col.Header);
            p.Format.Alignment = ToAlignment(col.HeaderAlign);
            p.Format.Font.Name = ctx.Layout.Styles.FontName;
            p.Format.Font.Size = col.HeaderFontSize;
            p.Format.Font.Bold = col.HeaderBold;
        }

        foreach (var detail in ctx.Details)
        {
            var row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Center;
            for (int i = 0; i < columns.Count && i < row.Cells.Count; i++)
            {
                var col = columns[i];
                var value = GetDetailFieldValue(detail, col.DataField);
                var cell = row.Cells[i];
                cell.VerticalAlignment = VerticalAlignment.Center;
                var p = cell.AddParagraph(value);
                p.Format.Alignment = ToAlignment(col.DataAlign);
                p.Format.Font.Name = ctx.Layout.Styles.FontName;
                p.Format.Font.Size = col.DataFontSize;
            }
        }
    }

    private static List<PdfTableColumn> GetToolTableColumns(PdfLayoutElement element)
    {
        var defaults = DefaultPdfLayoutFactory.Create().Elements
            .First(e => e.Id == "tool-table").Columns!
            .Where(c => c.Visible)
            .ToList();

        var source = (element.Columns ?? new List<PdfTableColumn>()).Where(c => c.Visible).ToList();
        if (source.Count == 0)
            return defaults;

        var merged = new List<PdfTableColumn>();
        for (int i = 0; i < source.Count; i++)
        {
            var src = source[i];
            var def = defaults.FirstOrDefault(d =>
                string.Equals(d.Header, src.Header, StringComparison.OrdinalIgnoreCase))
                ?? (i < defaults.Count ? defaults[i] : defaults[^1]);

            merged.Add(new PdfTableColumn
            {
                Id = src.Id,
                Header = string.IsNullOrWhiteSpace(src.Header) ? def.Header : src.Header,
                DataField = def.DataField,
                Width = src.Width > 0 ? src.Width : def.Width,
                WidthUnit = string.IsNullOrWhiteSpace(src.WidthUnit) ? def.WidthUnit : src.WidthUnit,
                Visible = src.Visible,
                HeaderFontSize = src.HeaderFontSize > 0 ? src.HeaderFontSize : def.HeaderFontSize,
                DataFontSize = src.DataFontSize > 0 ? src.DataFontSize : def.DataFontSize,
                HeaderBold = src.HeaderBold,
                HeaderAlign = src.HeaderAlign,
                DataAlign = src.DataAlign
            });
        }

        return merged;
    }

    private static void RenderStampTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        if (container is Section section)
        {
            var spacer = section.AddParagraph();
            spacer.Format.SpaceAfter = Unit.FromCentimeter(1);
        }

        var table = AddTable(container);
        table.Borders.Visible = false;
        var colWidth = element.Width / 3;
        for (int i = 0; i < 3; i++)
            table.AddColumn(Unit.FromCentimeter(colWidth));

        var row = table.AddRow();
        AddStampCell(row.Cells[0], ParagraphAlignment.Left, "CAM Programmer:", ctx.ViewModel.CamProgrammer ?? "",
            ctx.CamProgrammerStamp, ctx.ViewModel.ApprovedDate, ctx);
        AddStampCell(row.Cells[1], ParagraphAlignment.Center, "Approved by:", ctx.ViewModel.ApprovedBy ?? "",
            ctx.ApprovedByStamp, ctx.ViewModel.CamLeaderApprovedDate, ctx);
        AddStampCell(row.Cells[2], ParagraphAlignment.Right, "Tool Register By:", ctx.ToolRegisterByName ?? "",
            ctx.ToolRegisterStamp, ctx.ViewModel.ToolRegisterByDate, ctx);
    }

    private static void RenderGenericTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        var table = AddTable(container);
        ApplyTableBorder(table, element, ctx);

        var columns = element.Columns ?? new List<PdfTableColumn>();
        var totalWidth = columns.Where(c => c.Visible).Sum(c => c.Width);
        var scale = totalWidth > 0 ? element.Width / totalWidth : 1;

        foreach (var col in columns.Where(c => c.Visible))
        {
            var w = col.WidthUnit == "pt"
                ? Unit.FromPoint(col.Width * scale)
                : Unit.FromCentimeter(col.Width * scale);
            table.AddColumn(w);
        }

        if (columns.Any(c => c.Visible && !string.IsNullOrEmpty(c.Header)))
        {
            var headerRow = table.AddRow();
            for (int i = 0; i < columns.Count(c => c.Visible); i++)
            {
                var col = columns.Where(c => c.Visible).ElementAt(i);
                var cell = headerRow.Cells[i];
                ApplyCellBackground(cell, ctx.Layout.Styles.HeaderFill);
                var p = cell.AddParagraph(col.Header);
                p.Format.Font.Name = ctx.Layout.Styles.FontName;
                p.Format.Font.Size = col.HeaderFontSize;
                p.Format.Font.Bold = col.HeaderBold;
            }
        }

        foreach (var rowDef in element.Rows ?? new List<PdfTableRow>())
        {
            var row = table.AddRow();
            var visibleCols = columns.Where(c => c.Visible).ToList();
            for (int i = 0; i < rowDef.Cells.Count && i < visibleCols.Count; i++)
            {
                var cellDef = rowDef.Cells[i];
                var cell = row.Cells[i];
                if (cellDef.ColSpan > 1) cell.MergeRight = cellDef.ColSpan - 1;
                ApplyCellBackground(cell, cellDef.BackgroundColor);
                var p = cell.AddParagraph(ResolveCellText(cellDef, ctx));
                p.Format.Font.Name = ctx.Layout.Styles.FontName;
                p.Format.Font.Size = cellDef.FontSize;
                p.Format.Font.Bold = cellDef.Bold;
                p.Format.Alignment = ToAlignment(cellDef.Align);
            }
        }
    }

    private static Table CreateScaledColumnTable(DocumentObject container, PdfLayoutElement element, RenderContext ctx)
    {
        var table = AddTable(container);
        ApplyTableBorder(table, element, ctx);

        var columns = string.Equals(element.TableKind, "tool", StringComparison.OrdinalIgnoreCase)
            ? GetToolTableColumns(element)
            : (element.Columns ?? new List<PdfTableColumn>()).Where(c => c.Visible).ToList();
        if (columns.Count == 0)
            columns = DefaultPdfLayoutFactory.Create().Elements.First(e => e.Id == "tool-table").Columns!
                .Where(c => c.Visible).ToList();

        var originalWidthPt = columns.Sum(c =>
            c.WidthUnit == "pt" ? c.Width : Unit.FromCentimeter(c.Width).Point);
        var scale = originalWidthPt > 0 ? ctx.ContentWidth.Point / originalWidthPt : 1;

        foreach (var col in columns)
        {
            var width = col.WidthUnit == "pt"
                ? Unit.FromPoint(col.Width * scale)
                : Unit.FromCentimeter(col.Width * scale);
            table.AddColumn(width);
        }

        return table;
    }

    private static Table AddTable(DocumentObject container)
    {
        return container switch
        {
            Section s => s.AddTable(),
            TextFrame f => f.AddTable(),
            _ => throw new InvalidOperationException("Unsupported container for table")
        };
    }

    private static void ApplyTableBorder(Table table, PdfLayoutElement element, RenderContext ctx)
    {
        if (element.ShowBorder)
        {
            table.Borders.Width = Unit.FromPoint(ctx.Layout.Styles.BorderWidthPt);
            table.Borders.Color = Colors.Black;
        }
        else
        {
            table.Borders.Visible = false;
        }
    }

    private static void AddStampCell(Cell cell, ParagraphAlignment alignment, string label, string name,
        byte[]? stampBytes, DateTime? date, RenderContext ctx)
    {
        cell.VerticalAlignment = VerticalAlignment.Top;
        var labelParagraph = cell.AddParagraph();
        labelParagraph.Format.Alignment = alignment;
        labelParagraph.Format.Font.Name = ctx.Layout.Styles.FontName;
        labelParagraph.Format.Font.Size = 9;
        labelParagraph.Format.SpaceAfter = Unit.FromPoint(4);
        var labelText = labelParagraph.AddFormattedText(label);
        labelText.Bold = true;
        if (!string.IsNullOrWhiteSpace(name))
            labelParagraph.AddText($" {name}");

        var stampPath = WriteTempImage(stampBytes, ctx.TempFiles);
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
            dateParagraph.Format.Font.Name = ctx.Layout.Styles.FontName;
            dateParagraph.Format.Font.Size = 8;
            dateParagraph.Format.SpaceBefore = Unit.FromPoint(4);
        }
    }

    private static void AddImageToCell(Cell cell, string? path, Unit maxHeight)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        cell.VerticalAlignment = VerticalAlignment.Center;
        var paragraph = cell.AddParagraph();
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        var image = paragraph.AddImage(path);
        image.Height = maxHeight;
        image.LockAspectRatio = true;
    }

    private static string? WriteTempImage(byte[]? imageBytes, List<string> tempFiles)
    {
        if (imageBytes is not { Length: > 0 }) return null;
        var path = Path.Combine(Path.GetTempPath(), $"tooling-stamp-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, imageBytes);
        tempFiles.Add(path);
        return path;
    }

    private static string ResolveText(PdfLayoutElement element, RenderContext ctx)
    {
        if (!string.IsNullOrEmpty(element.DataBinding))
            return ResolveBinding(element.DataBinding, ctx) ?? element.Text ?? "";
        return element.Text ?? "";
    }

    private static string ResolveCellText(PdfTableCell cell, RenderContext ctx)
    {
        if (!string.IsNullOrEmpty(cell.DataBinding))
            return ResolveBinding(cell.DataBinding, ctx) ?? cell.Text ?? "";
        return cell.Text ?? "";
    }

    private static string? ResolveBinding(string binding, RenderContext ctx)
    {
        var vm = ctx.ViewModel;
        return binding.TrimEnd(':').Trim().ToLowerInvariant() switch
        {
            "toollistname" or "tool list" => vm.ToolListName,
            "partnumber" or "part number" => vm.PartNumber,
            "partdescription" or "part description" => vm.PartDescription,
            "operation" => vm.Operation,
            "revision" => vm.Revision,
            "projectcode" or "project code" => vm.ProjectCode,
            "machinename" or "machine" => vm.MachineName,
            "machineworkcenter" or "workcenter" => vm.MachineWorkcenter,
            "machinemodel" or "machine model" => vm.MachineModel,
            "camprogrammer" or "cam programmer" => vm.CamProgrammer,
            "approvedby" or "approved by" => vm.ApprovedBy,
            "toolregisterbyname" or "tool register by" => ctx.ToolRegisterByName,
            "mm" => "MM",
            _ => binding switch
            {
                "ToolListName" => vm.ToolListName,
                "PartNumber" => vm.PartNumber,
                "PartDescription" => vm.PartDescription,
                "Operation" => vm.Operation,
                "Revision" => vm.Revision,
                "ProjectCode" => vm.ProjectCode,
                "MachineName" => vm.MachineName,
                "MachineWorkcenter" => vm.MachineWorkcenter,
                "MachineModel" => vm.MachineModel,
                "CamProgrammer" => vm.CamProgrammer,
                "ApprovedBy" => vm.ApprovedBy,
                "ToolRegisterByName" => ctx.ToolRegisterByName,
                "MM" => "MM",
                _ => binding
            }
        };
    }

    private static string GetDetailFieldValue(ToolListDetailRow detail, string? field)
    {
        if (string.IsNullOrWhiteSpace(field)) return "";

        return field.Trim().ToLowerInvariant() switch
        {
            "toolnumber" or "tool no." or "tool no" => detail.ToolNumber ?? "",
            "tooldescription" or "tool name" => detail.ToolDescription ?? "",
            "consumablecode" or "consumable tool description" => detail.ConsumableCode ?? "",
            "supplier" or "tool supplier" => detail.Supplier ?? "",
            "holderextensioncode" or "tool holder" => detail.HolderExtensionCode ?? "",
            "diameter" or "tool diameter (d1)" => (detail.Diameter ?? 0).ToString("0.##"),
            "flutelength" or "flute length (l1)" => (detail.FluteLength ?? 0).ToString("0.##"),
            "protrusionlength" or "tool ext. length (l2)" => (detail.ProtrusionLength ?? 0).ToString("0.##"),
            "cornerradius" or "tool corner radius" => (detail.CornerRadius ?? 0).ToString("0.##"),
            "arborcode" or "arbor description (or equivalent specs)" or "arbor description" => detail.ArborCode ?? "",
            "toolpathtimeminutes" or "tool path time in minutes" => (detail.ToolPathTimeMinutes ?? 0).ToString("0.##"),
            "remarks" => detail.Remarks ?? "",
            _ => field switch
            {
                "ToolNumber" => detail.ToolNumber ?? "",
                "ToolDescription" => detail.ToolDescription ?? "",
                "ConsumableCode" => detail.ConsumableCode ?? "",
                "Supplier" => detail.Supplier ?? "",
                "HolderExtensionCode" => detail.HolderExtensionCode ?? "",
                "Diameter" => (detail.Diameter ?? 0).ToString("0.##"),
                "FluteLength" => (detail.FluteLength ?? 0).ToString("0.##"),
                "ProtrusionLength" => (detail.ProtrusionLength ?? 0).ToString("0.##"),
                "CornerRadius" => (detail.CornerRadius ?? 0).ToString("0.##"),
                "ArborCode" => detail.ArborCode ?? "",
                "ToolPathTimeMinutes" => (detail.ToolPathTimeMinutes ?? 0).ToString("0.##"),
                "Remarks" => detail.Remarks ?? "",
                _ => ""
            }
        };
    }

    private static string? ResolveImagePath(string? source, RenderContext ctx) => source switch
    {
        "logo" => ctx.LogoPath,
        "partImage" => ctx.PartImagePath,
        "toolSpecs" => ctx.ToolSpecsPath,
        _ => null
    };

    private static void ApplyTextFormat(Paragraph paragraph, PdfLayoutElement element, RenderContext ctx)
    {
        paragraph.Format.Alignment = ToAlignment(element.Align);
        paragraph.Format.Font.Name = ctx.Layout.Styles.FontName;
        paragraph.Format.Font.Size = element.FontSize;
        paragraph.Format.Font.Bold = element.Bold;
        paragraph.Format.Font.Italic = element.Italic;
        if (!string.IsNullOrEmpty(element.Color))
            paragraph.Format.Font.Color = ParseColor(element.Color);
    }

    private static void ApplyCellBackground(Cell cell, string? colorHex)
    {
        if (string.IsNullOrEmpty(colorHex)) return;
        cell.Shading.Color = ParseColor(colorHex);
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            var r = Convert.ToInt32(hex[..2], 16);
            var g = Convert.ToInt32(hex[2..4], 16);
            var b = Convert.ToInt32(hex[4..6], 16);
            return new Color((byte)r, (byte)g, (byte)b);
        }
        return Colors.White;
    }

    private static ParagraphAlignment ToAlignment(string align) => align.ToLower() switch
    {
        "center" => ParagraphAlignment.Center,
        "right" => ParagraphAlignment.Right,
        _ => ParagraphAlignment.Left
    };

    private sealed class RenderContext
    {
        public PdfLayoutDocument Layout { get; }
        public ToolListEditorViewModel ViewModel { get; }
        public IReadOnlyList<ToolListDetailRow> Details { get; }
        public byte[]? CamProgrammerStamp { get; }
        public byte[]? ApprovedByStamp { get; }
        public byte[]? ToolRegisterStamp { get; }
        public string? ToolRegisterByName { get; }
        public string? LogoPath { get; }
        public string? PartImagePath { get; }
        public string? ToolSpecsPath { get; }
        public List<string> TempFiles { get; }
        public Unit ContentWidth { get; }

        public RenderContext(
            PdfLayoutDocument layout,
            ToolListEditorViewModel viewModel,
            IReadOnlyList<ToolListDetailRow> details,
            byte[]? camProgrammerStamp,
            byte[]? approvedByStamp,
            byte[]? toolRegisterStamp,
            string? toolRegisterByName,
            string? logoPath,
            string? partImagePath,
            string? toolSpecsPath,
            List<string> tempFiles,
            Unit contentWidth)
        {
            Layout = layout;
            ViewModel = viewModel;
            Details = details;
            CamProgrammerStamp = camProgrammerStamp;
            ApprovedByStamp = approvedByStamp;
            ToolRegisterStamp = toolRegisterStamp;
            ToolRegisterByName = toolRegisterByName;
            LogoPath = logoPath;
            PartImagePath = partImagePath;
            ToolSpecsPath = toolSpecsPath;
            TempFiles = tempFiles;
            ContentWidth = contentWidth;
        }
    }
}
