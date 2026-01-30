using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}
