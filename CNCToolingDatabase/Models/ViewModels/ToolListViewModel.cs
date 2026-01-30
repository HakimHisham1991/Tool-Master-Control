namespace CNCToolingDatabase.Models.ViewModels;

public class ToolListItemViewModel
{
    public int Id { get; set; }
    public string ToolListName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public int NumberOfTooling { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Status { get; set; } = "Available";
    public string? LockedBy { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? EditingDuration { get; set; }
    public bool CanEdit { get; set; } = true;
}

public class ToolListDatabaseViewModel
{
    public List<ToolListItemViewModel> ToolLists { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 250;
}
