using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public interface IToolCodeService
{
    Task<ToolCodeListViewModel> GetToolCodesAsync(
        string? searchTerm,
        string? consumableCodeFilter,
        string? diameterFilter,
        string? arborCodeFilter,
        string? holderExtensionFilter,
        string? partNumberFilter,
        string? toolListNameFilter,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize);
}
