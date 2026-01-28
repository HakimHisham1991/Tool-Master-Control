namespace CNCToolingDatabase.Models;

public class MachineModel
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
