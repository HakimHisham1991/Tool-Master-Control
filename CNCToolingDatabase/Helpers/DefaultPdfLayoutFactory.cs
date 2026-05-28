using CNCToolingDatabase.Models.PdfLayout;

namespace CNCToolingDatabase.Helpers;

public static class DefaultPdfLayoutFactory
{
    public const string DefaultLayoutName = "Master Tooling List (Default)";

    public static PdfLayoutDocument Create()
    {
        var contentWidth = 26.7;
        var doc = new PdfLayoutDocument
        {
            PageSetup = new PdfPageSetup(),
            Styles = new PdfLayoutStyles(),
            Version = 3
        };

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "header-logo",
            Type = "image",
            LayoutMode = "absolute",
            X = 0, Y = 0, Width = 2.8, Height = 1.5,
            ImageSource = "logo",
            LockAspectRatio = true
        });

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "header-title",
            Type = "text",
            LayoutMode = "absolute",
            X = 2.8, Y = 0, Width = contentWidth - 2.8, Height = 1.5,
            Text = "Master Tooling List",
            FontSize = 22,
            Bold = true,
            Align = "center"
        });

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "specs-table",
            Type = "table",
            TableKind = "specs",
            LayoutMode = "flow",
            GapBeforeCm = 0.21,
            X = 0, Y = 1.7, Width = contentWidth, Height = 4.2,
            ShowBorder = true,
            Columns = new List<PdfTableColumn>
            {
                new() { Header = "Label", Width = 4, WidthUnit = "cm", Visible = false },
                new() { Header = "Value", Width = contentWidth - 4, WidthUnit = "cm", Visible = false }
            },
            Rows = new List<PdfTableRow>
            {
                SpecsRow("Tool List:", "ToolListName"),
                SpecsRow("Part Number:", "PartNumber"),
                SpecsRow("Part Description:", "PartDescription"),
                SpecsRow("Operation:", "Operation"),
                SpecsRow("Revision:", "Revision"),
                SpecsRow("Project Code:", "ProjectCode"),
                SpecsRow("Machine:", "MachineName"),
                SpecsRow("Workcenter:", "MachineWorkcenter"),
                SpecsRow("Machine Model:", "MachineModel")
            }
        });

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "image-row",
            Type = "table",
            TableKind = "imageRow",
            LayoutMode = "flow",
            X = 0, Y = 5.5, Width = contentWidth, Height = 1.2,
            ShowBorder = true,
            Columns = CreateToolTableColumns()
        });

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "tool-table",
            Type = "table",
            TableKind = "tool",
            LayoutMode = "flow",
            DataSource = "tools",
            X = 0, Y = 6.8, Width = contentWidth, Height = 6,
            ShowBorder = true,
            Columns = CreateToolTableColumns()
        });

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "stamp-section",
            Type = "table",
            TableKind = "stamps",
            LayoutMode = "flow",
            X = 0, Y = 15, Width = contentWidth, Height = 3,
            ShowBorder = false,
            Columns = new List<PdfTableColumn>
            {
                new() { Width = contentWidth / 3, WidthUnit = "cm" },
                new() { Width = contentWidth / 3, WidthUnit = "cm" },
                new() { Width = contentWidth / 3, WidthUnit = "cm" }
            }
        });

        doc.Elements.Add(new PdfLayoutElement
        {
            Id = "footer",
            Type = "text",
            LayoutMode = "flow",
            X = 0, Y = 18.2, Width = contentWidth, Height = 0.5,
            Text = "Page {page}",
            FontSize = 9,
            Align = "center",
            DataBinding = "pageNumber"
        });

        return doc;
    }

    private static PdfTableRow SpecsRow(string label, string binding)
    {
        return new PdfTableRow
        {
            Cells = new List<PdfTableCell>
            {
                new() { Text = label, IsLabel = true, Bold = true, FontSize = 8, BackgroundColor = "#CCFFFF" },
                new() { DataBinding = binding, FontSize = 8 }
            }
        };
    }

    private static List<PdfTableColumn> CreateToolTableColumns() => new()
    {
        Col("Tool No.", "ToolNumber", 45, "pt"),
        Col("Tool Name", "ToolDescription", 2.8, "cm"),
        Col("Consumable Tool Description", "ConsumableCode", 2.8, "cm"),
        Col("Tool Supplier", "Supplier", 1.7, "cm"),
        Col("Tool Holder", "HolderExtensionCode", 1.7, "cm"),
        Col("Tool Diameter (D1)", "Diameter", 40, "pt"),
        Col("Flute Length (L1)", "FluteLength", 40, "pt"),
        Col("Tool Ext. Length (L2)", "ProtrusionLength", 40, "pt"),
        Col("Tool Corner Radius", "CornerRadius", 45, "pt"),
        Col("Arbor Description (or equivalent specs)", "ArborCode", 2.8, "cm"),
        Col("Tool Path Time in Minutes", "ToolPathTimeMinutes", 55, "pt"),
        Col("Remarks", "Remarks", 2.2, "cm")
    };

    private static PdfTableColumn Col(string header, string field, double width, string unit) => new()
    {
        Header = header,
        DataField = field,
        Width = width,
        WidthUnit = unit,
        HeaderFontSize = 6,
        DataFontSize = 6,
        HeaderBold = true
    };
}
