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
    /// <summary>User id of the verifier; their stamp is shown in Verified by.</summary>
    public int? ApprovedByUserId { get; set; }
    /// <summary>Date when the tool list was approved (stamp applied).</summary>
    public DateTime? ApprovedDate { get; set; }
    /// <summary>User id of CAM Leader approver; their stamp is shown in Approved by.</summary>
    public int? CamLeaderApprovedByUserId { get; set; }
    public DateTime? CamLeaderApprovedDate { get; set; }
    /// <summary>User id of Tool Register approver; their stamp is shown in Tool Register By.</summary>
    public int? ToolRegisterByUserId { get; set; }
    public DateTime? ToolRegisterByDate { get; set; }
    public string CamProgrammer { get; set; } = string.Empty;
    public int? MaterialSpecId { get; set; }
    public MaterialSpec? MaterialSpec { get; set; }
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
