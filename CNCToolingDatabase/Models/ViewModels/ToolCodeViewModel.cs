namespace CNCToolingDatabase.Models.ViewModels;

public class ToolCodeViewModel
{
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
    public string PartNumber { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string ToolListName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string MachineWorkcenter { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class ToolCodeListViewModel
{
    public List<ToolCodeViewModel> Tools { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? ConsumableCodeFilter { get; set; }
    public string? DiameterFilter { get; set; }
    public string? ArborCodeFilter { get; set; }
    public string? HolderExtensionFilter { get; set; }
    public string? PartNumberFilter { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 100;
    
    public List<string> AvailableConsumableCodes { get; set; } = new();
    public List<string> AvailableDiameters { get; set; } = new();
    public List<string> AvailableArborCodes { get; set; } = new();
    public List<string> AvailableHolderExtensions { get; set; } = new();
    public List<string> AvailablePartNumbers { get; set; } = new();
}
