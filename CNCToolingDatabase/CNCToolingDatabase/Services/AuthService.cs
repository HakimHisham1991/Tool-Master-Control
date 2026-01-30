using CNCToolingDatabase.Models;
using CNCToolingDatabase.Repositories;

namespace CNCToolingDatabase.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private const string UserIdSessionKey = "UserId";
    private const string UsernameSessionKey = "Username";
    private const string DisplayNameSessionKey = "DisplayName";
    
    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        if (await _userRepository.ValidateCredentialsAsync(username, password))
        {
            return await _userRepository.GetByUsernameAsync(username);
        }
        return null;
    }
    
    public async Task<User?> GetCurrentUserAsync(HttpContext context)
    {
        var userId = context.Session.GetInt32(UserIdSessionKey);
        if (userId.HasValue)
        {
            return await _userRepository.GetByIdAsync(userId.Value);
        }
        return null;
    }
    
    public void SetUserSession(HttpContext context, User user)
    {
        context.Session.SetInt32(UserIdSessionKey, user.Id);
        context.Session.SetString(UsernameSessionKey, user.Username);
        context.Session.SetString(DisplayNameSessionKey, user.DisplayName);
    }
    
    public void ClearUserSession(HttpContext context)
    {
        context.Session.Clear();
    }
    
    public bool IsAuthenticated(HttpContext context)
    {
        return context.Session.GetInt32(UserIdSessionKey).HasValue;
    }
}
