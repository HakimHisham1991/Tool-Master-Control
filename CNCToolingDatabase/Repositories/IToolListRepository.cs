using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Repositories;

public interface IToolListRepository
{
    Task<List<ToolListHeader>> GetAllHeadersAsync();
    Task<Dictionary<int, int>> GetToolCountsByHeaderIdsAsync(IEnumerable<int> headerIds);
    Task<ToolListHeader?> GetHeaderByIdAsync(int id);
    Task<ToolListHeader?> GetHeaderWithDetailsAsync(int id);
    Task<ToolListHeader?> GetByPartNumberAndOperationAsync(string partNumber, string operation);
    Task<ToolListHeader> CreateHeaderAsync(ToolListHeader header);
    Task UpdateHeaderAsync(ToolListHeader header);
    Task DeleteHeaderAsync(int id);
    
    Task<bool> AcquireLockAsync(int headerId, string username);
    Task<bool> ReleaseLockAsync(int headerId, string username);
    Task UpdateHeartbeatAsync(int headerId, string username);
    Task ReleaseExpiredLocksAsync(TimeSpan timeout);
    
    Task SaveDetailsAsync(int headerId, List<ToolListDetail> details);
}
