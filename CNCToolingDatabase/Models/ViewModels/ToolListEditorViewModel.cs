namespace CNCToolingDatabase.Models.ViewModels;

public class ToolListDetailRow
{
    public int Id { get; set; }
    public string ToolNumber { get; set; } = string.Empty;
    public string ToolDescription { get; set; } = string.Empty;
    public string ConsumableCode { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string HolderExtensionCode { get; set; } = string.Empty;
    public decimal? Diameter { get; set; }
    public decimal? FluteLength { get; set; }
    public decimal? ProtrusionLength { get; set; }
    public decimal? CornerRadius { get; set; }
    public string ArborCode { get; set; } = string.Empty;
    public decimal? ToolPathTimeMinutes { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class ToolListEditorViewModel
{
    public int? Id { get; set; }
    public string ToolListName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string PartDescription { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string MachineWorkcenter { get; set; } = string.Empty;
    public string MachineModel { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public int? CamLeaderApprovedByUserId { get; set; }
    public DateTime? CamLeaderApprovedDate { get; set; }
    public int? ToolRegisterByUserId { get; set; }
    public DateTime? ToolRegisterByDate { get; set; }
    public string CamProgrammer { get; set; } = string.Empty;
    public int? MaterialSpecId { get; set; }
    public string Material { get; set; } = string.Empty;
    public List<ToolListDetailRow> Details { get; set; } = new();
    public bool IsReadOnly { get; set; }
    public string? LockedBy { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class OpenToolListViewModel
{
    public List<ToolListItemViewModel> AvailableToolLists { get; set; } = new();
    public string? SearchTerm { get; set; }
}

public class SaveToolListRequest
{
    public int? Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string MachineWorkcenter { get; set; } = string.Empty;
    public string MachineModel { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public string CamProgrammer { get; set; } = string.Empty;
    public int? MaterialSpecId { get; set; }
    public List<ToolListDetailRow> Details { get; set; } = new();
}
