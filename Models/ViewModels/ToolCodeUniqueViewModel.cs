namespace CNCToolingDatabase.Models.ViewModels;

public class ToolCodeUniqueItemViewModel
{
    public int No { get; set; }
    public string SystemToolName { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal Diameter { get; set; }
    public decimal FluteLength { get; set; }
    public decimal CornerRadius { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class ToolCodeUniqueListViewModel
{
    public List<ToolCodeUniqueItemViewModel> Tools { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? ConsumableCodeFilter { get; set; }
    public string? SupplierFilter { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 100;
    public List<string> AvailableConsumableCodes { get; set; } = new();
    public List<string> AvailableSuppliers { get; set; } = new();
}
