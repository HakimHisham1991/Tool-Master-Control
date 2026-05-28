namespace CNCToolingDatabase.Models;

public class PdfLayoutConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LayoutJson { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}
