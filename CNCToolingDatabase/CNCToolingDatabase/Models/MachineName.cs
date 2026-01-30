namespace CNCToolingDatabase.Models;

public class MachineName
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Workcenter { get; set; } = string.Empty;
    public int? MachineModelId { get; set; }
    public MachineModel? MachineModel { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
