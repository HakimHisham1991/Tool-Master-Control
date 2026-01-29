namespace CNCToolingDatabase.Models;

/// <summary>
/// User-maintained reference database. Not extracted from tool lists.
/// Tools are never hard-deleted; Id is the running "No." that always increases.
/// </summary>
public class ToolCodeUnique
{
    public int Id { get; set; }
    public string SystemToolName { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal Diameter { get; set; }
    public decimal FluteLength { get; set; }
    public decimal CornerRadius { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
}
