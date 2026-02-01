using System.Globalization;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Compare password: exact match, or if both parse as numbers compare numeric value (so "123" matches "123.0" or "123,0").</summary>
    private static bool PasswordsMatch(string? stored, string? entered)
    {
        var s = (stored ?? "").Trim();
        var e = (entered ?? "").Trim();
        if (s == e) return true;
        if (TryParseNumber(s, out var ds) && TryParseNumber(e, out var de))
            return Math.Abs(ds - de) < 0.0001;
        return false;
    }

    private static bool TryParseNumber(string value, out double result)
    {
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)) return true;
        return false;
    }

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var active = await _context.Users.Where(u => u.IsActive).ToListAsync();
        return active.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null) return false;
        return PasswordsMatch(user.Password, password);
    }
}
