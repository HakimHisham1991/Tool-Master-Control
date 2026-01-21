namespace CNCToolingDatabase.Models;

public class ToolListDetail
{
    public int Id { get; set; }
    public int ToolListHeaderId { get; set; }
    public string ToolNumber { get; set; } = string.Empty;
    public string ToolDescription { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string HolderExtensionCode { get; set; } = string.Empty;
    public decimal Diameter { get; set; }
    public decimal FluteLength { get; set; }
    public decimal ProtrusionLength { get; set; }
    public decimal CornerRadius { get; set; }
    public string ArborCode { get; set; } = string.Empty;
    
    public virtual ToolListHeader? Header { get; set; }
}
