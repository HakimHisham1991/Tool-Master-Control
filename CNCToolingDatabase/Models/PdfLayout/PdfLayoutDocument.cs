using System.Text.Json;
using System.Text.Json.Serialization;

namespace CNCToolingDatabase.Models.PdfLayout;

public class PdfLayoutDocument
{
    public PdfPageSetup PageSetup { get; set; } = new();
    public PdfLayoutStyles Styles { get; set; } = new();
    public List<PdfLayoutElement> Elements { get; set; } = new();
    public int Version { get; set; } = 3;

    public static PdfLayoutDocument? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<PdfLayoutDocument>(json, JsonOptions);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };
}

public class PdfPageSetup
{
    public string Format { get; set; } = "A4";
    public string Orientation { get; set; } = "Landscape";
    public double MarginCm { get; set; } = 1.5;
}

public class PdfLayoutStyles
{
    public string FontName { get; set; } = "Arial";
    public string HeaderFill { get; set; } = "#CCFFFF";
    public double BorderWidthPt { get; set; } = 0.5;
}

public class PdfLayoutElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "text";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 5;
    public double Height { get; set; } = 1;
    public int ZIndex { get; set; }
    public bool Visible { get; set; } = true;
    public string LayoutMode { get; set; } = "absolute";
    /// <summary>Extra vertical gap before this element in flow layout (cm). Canvas Y is editor-only.</summary>
    public double GapBeforeCm { get; set; }

    public string? Text { get; set; }
    public string? DataBinding { get; set; }
    public double FontSize { get; set; } = 8;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public string Align { get; set; } = "left";
    public string? Color { get; set; }
    public string? BackgroundColor { get; set; }
    public bool ShowBorder { get; set; }

    public string? ImageSource { get; set; }
    public bool LockAspectRatio { get; set; } = true;

    public string? TableKind { get; set; }
    public List<PdfTableColumn>? Columns { get; set; }
    public List<PdfTableRow>? Rows { get; set; }
    public string? DataSource { get; set; }
}

public class PdfTableColumn
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Header { get; set; } = string.Empty;
    public string? DataField { get; set; }
    public double Width { get; set; } = 2;
    public string WidthUnit { get; set; } = "cm";
    public bool Visible { get; set; } = true;
    public double HeaderFontSize { get; set; } = 6;
    public double DataFontSize { get; set; } = 6;
    public bool HeaderBold { get; set; } = true;
    public string HeaderAlign { get; set; } = "center";
    public string DataAlign { get; set; } = "center";
    public int MergeRight { get; set; }
}

public class PdfTableRow
{
    public List<PdfTableCell> Cells { get; set; } = new();
}

public class PdfTableCell
{
    public string? Text { get; set; }
    public string? Label { get; set; }
    public string? DataBinding { get; set; }
    public bool IsLabel { get; set; }
    public int ColSpan { get; set; } = 1;
    public double FontSize { get; set; } = 8;
    public bool Bold { get; set; }
    public string Align { get; set; } = "left";
    public string? BackgroundColor { get; set; }
}
