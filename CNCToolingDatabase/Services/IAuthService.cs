using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Services;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> GetCurrentUserAsync(HttpContext context);
    void SetUserSession(HttpContext context, User user);
    void ClearUserSession(HttpContext context);
    bool IsAuthenticated(HttpContext context);
}
