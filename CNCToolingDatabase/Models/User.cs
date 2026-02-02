namespace CNCToolingDatabase.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    /// <summary>Stamp/signature image (e.g. jpg, png, gif).</summary>
    public byte[]? Stamp { get; set; }
}
