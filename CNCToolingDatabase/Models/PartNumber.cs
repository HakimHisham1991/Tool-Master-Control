namespace CNCToolingDatabase.Models;

public class PartNumber
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ProjectCodeId { get; set; }
    public ProjectCode? ProjectCode { get; set; }
    public string? PartRev { get; set; }
    public string? DrawingRev { get; set; }
    public int? MaterialSpecId { get; set; }
    public MaterialSpec? MaterialSpec { get; set; }
    public string? RefDrawing { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
