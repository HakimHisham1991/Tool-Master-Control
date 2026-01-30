namespace CNCToolingDatabase.Models;

public class ToolListHeader
{
    public int Id { get; set; }
    public string ToolListName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string MachineWorkcenter { get; set; } = string.Empty;
    public string MachineModel { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public string CamProgrammer { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    
    public string? LockedBy { get; set; }
    public DateTime? LockStartTime { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    
    public virtual ICollection<ToolListDetail> Details { get; set; } = new List<ToolListDetail>();
    
    public void GenerateToolListName()
    {
        ToolListName = $"{PartNumber}_{Operation}_{Revision}";
    }
}
