using CNCToolingDatabase.Models;
using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public interface IToolListService
{
    Task<ToolListDatabaseViewModel> GetToolListsAsync(
        string? searchTerm,
        string? toolListNameFilter,
        string? partNumberFilter,
        string? operationFilter,
        string? revisionFilter,
        string? numberOfToolingFilter,
        string? projectCodeFilter,
        string? machineNameFilter,
        string? machineWorkcenterFilter,
        string? machineModelFilter,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize,
        string currentUsername);
    
    Task<ToolListEditorViewModel> GetToolListForEditAsync(int id, string username);
    Task<ToolListEditorViewModel> CreateNewToolListAsync();
    Task<(bool Success, string Message, int? Id)> SaveToolListAsync(SaveToolListRequest request, string username);
    Task<bool> ReleaseToolListLockAsync(int id, string username);
    Task UpdateHeartbeatAsync(int id, string username);
    Task<List<ToolListItemViewModel>> GetAvailableToolListsAsync(string? searchTerm);
}
