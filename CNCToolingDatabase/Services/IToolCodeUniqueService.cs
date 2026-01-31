using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public interface IToolCodeUniqueService
{
    Task<ToolCodeUniqueListViewModel> GetToolCodesAsync(
        string? searchTerm,
        string? systemToolNameFilter,
        string? consumableCodeFilter,
        string? supplierFilter,
        string? diameterFilter,
        string? fluteLengthFilter,
        string? cornerRadiusFilter,
        string? createdDateFilter,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize);
}
