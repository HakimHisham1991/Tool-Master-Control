namespace CNCToolingDatabase.Models;

public class MaterialSpec
{
    public int Id { get; set; }
    public string Spec { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
