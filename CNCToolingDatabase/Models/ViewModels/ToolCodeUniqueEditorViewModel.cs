namespace CNCToolingDatabase.Models.ViewModels;

public class ToolCodeUniqueEditorViewModel
{
    public int? Id { get; set; }
    public string SystemToolName { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal Diameter { get; set; }
    public decimal FluteLength { get; set; }
    public decimal CornerRadius { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public List<string> AvailableSuppliers { get; set; } = new List<string>();
}

public class SaveToolCodeUniqueRequest
{
    public int? Id { get; set; }
    public string SystemToolName { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal Diameter { get; set; }
    public decimal FluteLength { get; set; }
    public decimal CornerRadius { get; set; }
}
