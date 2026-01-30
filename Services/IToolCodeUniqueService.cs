using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public interface IToolCodeUniqueService
{
    Task<ToolCodeUniqueListViewModel> GetToolCodesAsync(
        string? searchTerm,
        string? consumableCodeFilter,
        string? supplierFilter,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize);
}
