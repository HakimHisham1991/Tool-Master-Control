using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Repositories;

public interface IToolMasterRepository
{
    Task<List<ToolMaster>> GetAllAsync();
    Task<ToolMaster?> GetByConsumableCodeAsync(string consumableCode);
    Task UpdateOrCreateAsync(ToolMaster toolMaster);
    Task UpdateFromDetailAsync(ToolListDetail detail);
}
