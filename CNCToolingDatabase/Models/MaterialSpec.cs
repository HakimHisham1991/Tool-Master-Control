namespace CNCToolingDatabase.Models;

public class MaterialSpec
{
    public int Id { get; set; }
    /// <summary>Material Specification (On Drawing)</summary>
    public string Spec { get; set; } = string.Empty;
    /// <summary>Material Specification (Purchased)</summary>
    public string? MaterialSpecPurchased { get; set; }
    /// <summary>General Name</summary>
    public string Material { get; set; } = string.Empty;
    /// <summary>Material Supply Condition (Purchased)</summary>
    public string? MaterialSupplyConditionPurchased { get; set; }
    /// <summary>Material Type</summary>
    public string? MaterialType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
