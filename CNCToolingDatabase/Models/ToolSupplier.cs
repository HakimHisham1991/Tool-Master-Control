namespace CNCToolingDatabase.Models;

public class ToolSupplier
{
    public int Id { get; set; }
    /// <summary>Tool Supplier name (from Excel column)</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Website URL (from Excel column)</summary>
    public string? Website { get; set; }
    /// <summary>Status exactly as in Excel column (no conversion)</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}
