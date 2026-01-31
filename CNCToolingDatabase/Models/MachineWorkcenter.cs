namespace CNCToolingDatabase.Models;

public class MachineWorkcenter
{
    public int Id { get; set; }
    public string Workcenter { get; set; } = string.Empty;
    public string? Axis { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
