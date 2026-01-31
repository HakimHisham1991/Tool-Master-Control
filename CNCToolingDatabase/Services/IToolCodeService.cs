using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public interface IToolCodeService
{
    Task<ToolCodeListViewModel> GetToolCodesAsync(
        string? searchTerm,
        string? toolNumberFilter,
        string? toolDescriptionFilter,
        string? consumableCodeFilter,
        string? supplierFilter,
        string? holderExtensionFilter,
        string? diameterFilter,
        string? fluteLengthFilter,
        string? protrusionLengthFilter,
        string? cornerRadiusFilter,
        string? arborCodeFilter,
        string? partNumberFilter,
        string? operationFilter,
        string? revisionFilter,
        string? toolListNameFilter,
        string? projectCodeFilter,
        string? machineNameFilter,
        string? machineWorkcenterFilter,
        string? createdByFilter,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize);
}
