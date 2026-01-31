using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public class ToolCodeService : IToolCodeService
{
    private readonly ApplicationDbContext _context;
    
    public ToolCodeService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ToolCodeListViewModel> GetToolCodesAsync(
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
        int pageSize)
    {
        var query = from detail in _context.ToolListDetails
                    join header in _context.ToolListHeaders on detail.ToolListHeaderId equals header.Id
                    select new ToolCodeViewModel
                    {
                        ToolNumber = detail.ToolNumber,
                        ToolDescription = detail.ToolDescription,
                        ConsumableCode = detail.ConsumableCode,
                        Supplier = detail.Supplier,
                        HolderExtensionCode = detail.HolderExtensionCode,
                        Diameter = detail.Diameter,
                        FluteLength = detail.FluteLength,
                        ProtrusionLength = detail.ProtrusionLength,
                        CornerRadius = detail.CornerRadius,
                        ArborCode = detail.ArborCode,
                        PartNumber = header.PartNumber,
                        Operation = header.Operation,
                        Revision = header.Revision,
                        ToolListName = header.ToolListName,
                        ProjectCode = header.ProjectCode,
                        MachineName = header.MachineName,
                        MachineWorkcenter = header.MachineWorkcenter,
                        CreatedBy = header.CreatedBy,
                        CreatedDate = header.CreatedDate,
                        LastModifiedDate = header.LastModifiedDate
                    };
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(t => 
                t.ToolNumber.ToLower().Contains(term) ||
                t.ToolDescription.ToLower().Contains(term) ||
                t.ConsumableCode.ToLower().Contains(term) ||
                t.Supplier.ToLower().Contains(term) ||
                t.PartNumber.ToLower().Contains(term));
        }
        
        if (!string.IsNullOrWhiteSpace(toolNumberFilter))
            query = query.Where(t => t.ToolNumber == toolNumberFilter);
        if (!string.IsNullOrWhiteSpace(toolDescriptionFilter))
            query = query.Where(t => t.ToolDescription != null && t.ToolDescription.Contains(toolDescriptionFilter));
        if (!string.IsNullOrWhiteSpace(consumableCodeFilter))
            query = query.Where(t => t.ConsumableCode == consumableCodeFilter);
        if (!string.IsNullOrWhiteSpace(supplierFilter))
            query = query.Where(t => t.Supplier == supplierFilter);
        if (!string.IsNullOrWhiteSpace(holderExtensionFilter))
            query = query.Where(t => t.HolderExtensionCode == holderExtensionFilter);
        if (!string.IsNullOrWhiteSpace(diameterFilter) && decimal.TryParse(diameterFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dia))
            query = query.Where(t => t.Diameter == dia);
        if (!string.IsNullOrWhiteSpace(fluteLengthFilter) && decimal.TryParse(fluteLengthFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fl))
            query = query.Where(t => t.FluteLength == fl);
        if (!string.IsNullOrWhiteSpace(protrusionLengthFilter) && decimal.TryParse(protrusionLengthFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pl))
            query = query.Where(t => t.ProtrusionLength == pl);
        if (!string.IsNullOrWhiteSpace(cornerRadiusFilter) && decimal.TryParse(cornerRadiusFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cr))
            query = query.Where(t => t.CornerRadius == cr);
        if (!string.IsNullOrWhiteSpace(arborCodeFilter))
            query = query.Where(t => t.ArborCode == arborCodeFilter);
        if (!string.IsNullOrWhiteSpace(partNumberFilter))
            query = query.Where(t => t.PartNumber == partNumberFilter);
        if (!string.IsNullOrWhiteSpace(operationFilter))
            query = query.Where(t => t.Operation == operationFilter);
        if (!string.IsNullOrWhiteSpace(revisionFilter))
            query = query.Where(t => t.Revision == revisionFilter);
        if (!string.IsNullOrWhiteSpace(toolListNameFilter))
            query = query.Where(t => t.ToolListName == toolListNameFilter);
        if (!string.IsNullOrWhiteSpace(projectCodeFilter))
            query = query.Where(t => t.ProjectCode == projectCodeFilter);
        if (!string.IsNullOrWhiteSpace(machineNameFilter))
            query = query.Where(t => t.MachineName == machineNameFilter);
        if (!string.IsNullOrWhiteSpace(machineWorkcenterFilter))
            query = query.Where(t => t.MachineWorkcenter == machineWorkcenterFilter);
        if (!string.IsNullOrWhiteSpace(createdByFilter))
            query = query.Where(t => t.CreatedBy == createdByFilter);
        
        query = ApplySort(query, sortColumn, sortDirection);
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var tools = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var allDetails = await _context.ToolListDetails.ToListAsync();
        var allHeaders = await _context.ToolListHeaders.ToListAsync();
        
        return new ToolCodeListViewModel
        {
            Tools = tools,
            SearchTerm = searchTerm,
            ToolNumberFilter = toolNumberFilter,
            ToolDescriptionFilter = toolDescriptionFilter,
            ConsumableCodeFilter = consumableCodeFilter,
            SupplierFilter = supplierFilter,
            HolderExtensionFilter = holderExtensionFilter,
            DiameterFilter = diameterFilter,
            FluteLengthFilter = fluteLengthFilter,
            ProtrusionLengthFilter = protrusionLengthFilter,
            CornerRadiusFilter = cornerRadiusFilter,
            ArborCodeFilter = arborCodeFilter,
            PartNumberFilter = partNumberFilter,
            OperationFilter = operationFilter,
            RevisionFilter = revisionFilter,
            ToolListNameFilter = toolListNameFilter,
            ProjectCodeFilter = projectCodeFilter,
            MachineNameFilter = machineNameFilter,
            MachineWorkcenterFilter = machineWorkcenterFilter,
            CreatedByFilter = createdByFilter,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize,
            AvailableToolNumbers = allDetails.Select(d => d.ToolNumber).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableToolDescriptions = allDetails.Select(d => d.ToolDescription).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableConsumableCodes = allDetails.Select(d => d.ConsumableCode).Distinct().OrderBy(x => x).ToList(),
            AvailableSuppliers = allDetails.Select(d => d.Supplier).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableHolderExtensions = allDetails.Select(d => d.HolderExtensionCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableDiameters = allDetails.Select(d => d.Diameter.ToString("0.##")).Distinct().OrderBy(x => x).ToList(),
            AvailableFluteLengths = allDetails.Select(d => d.FluteLength.ToString("0.##")).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList(),
            AvailableProtrusionLengths = allDetails.Select(d => d.ProtrusionLength.ToString("0.##")).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList(),
            AvailableCornerRadii = allDetails.Select(d => d.CornerRadius.ToString("0.##")).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList(),
            AvailableArborCodes = allDetails.Select(d => d.ArborCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailablePartNumbers = allHeaders.Select(h => h.PartNumber).Distinct().OrderBy(x => x).ToList(),
            AvailableOperations = allHeaders.Select(h => h.Operation).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableRevisions = allHeaders.Select(h => h.Revision).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableToolListNames = allHeaders.Select(h => h.ToolListName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableProjectCodes = allHeaders.Select(h => h.ProjectCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableMachineNames = allHeaders.Select(h => h.MachineName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableMachineWorkcenters = allHeaders.Select(h => h.MachineWorkcenter).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableCreatedBys = allHeaders.Select(h => h.CreatedBy).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList()
        };
    }
    
    private IQueryable<ToolCodeViewModel> ApplySort(IQueryable<ToolCodeViewModel> query, string? column, string? direction)
    {
        var isDescending = direction?.ToLower() == "desc";
        
        return column?.ToLower() switch
        {
            "no" => isDescending
                ? query.OrderByDescending(t => t.PartNumber).ThenByDescending(t => t.Operation).ThenByDescending(t => t.ToolNumber)
                : query.OrderBy(t => t.PartNumber).ThenBy(t => t.Operation).ThenBy(t => t.ToolNumber),
            "toolnumber" => isDescending ? query.OrderByDescending(t => t.ToolNumber) : query.OrderBy(t => t.ToolNumber),
            "tooldescription" => isDescending ? query.OrderByDescending(t => t.ToolDescription) : query.OrderBy(t => t.ToolDescription),
            "consumablecode" => isDescending ? query.OrderByDescending(t => t.ConsumableCode) : query.OrderBy(t => t.ConsumableCode),
            "supplier" => isDescending ? query.OrderByDescending(t => t.Supplier) : query.OrderBy(t => t.Supplier),
            "holderextensioncode" => isDescending ? query.OrderByDescending(t => t.HolderExtensionCode) : query.OrderBy(t => t.HolderExtensionCode),
            "diameter" => isDescending ? query.OrderByDescending(t => t.Diameter) : query.OrderBy(t => t.Diameter),
            "flutelength" => isDescending ? query.OrderByDescending(t => t.FluteLength) : query.OrderBy(t => t.FluteLength),
            "protrusionlength" => isDescending ? query.OrderByDescending(t => t.ProtrusionLength) : query.OrderBy(t => t.ProtrusionLength),
            "cornerradius" => isDescending ? query.OrderByDescending(t => t.CornerRadius) : query.OrderBy(t => t.CornerRadius),
            "arborcode" => isDescending ? query.OrderByDescending(t => t.ArborCode) : query.OrderBy(t => t.ArborCode),
            "partnumber" => isDescending ? query.OrderByDescending(t => t.PartNumber) : query.OrderBy(t => t.PartNumber),
            "operation" => isDescending ? query.OrderByDescending(t => t.Operation) : query.OrderBy(t => t.Operation),
            "revision" => isDescending ? query.OrderByDescending(t => t.Revision) : query.OrderBy(t => t.Revision),
            "toollistname" => isDescending ? query.OrderByDescending(t => t.ToolListName) : query.OrderBy(t => t.ToolListName),
            "projectcode" => isDescending ? query.OrderByDescending(t => t.ProjectCode) : query.OrderBy(t => t.ProjectCode),
            "machinename" => isDescending ? query.OrderByDescending(t => t.MachineName) : query.OrderBy(t => t.MachineName),
            "machineworkcenter" => isDescending ? query.OrderByDescending(t => t.MachineWorkcenter) : query.OrderBy(t => t.MachineWorkcenter),
            "createdby" => isDescending ? query.OrderByDescending(t => t.CreatedBy) : query.OrderBy(t => t.CreatedBy),
            "createddate" => isDescending ? query.OrderByDescending(t => t.CreatedDate) : query.OrderBy(t => t.CreatedDate),
            "lastmodifieddate" => isDescending ? query.OrderByDescending(t => t.LastModifiedDate) : query.OrderBy(t => t.LastModifiedDate),
            _ => query.OrderBy(t => t.PartNumber).ThenBy(t => t.Operation).ThenBy(t => t.ToolNumber)
        };
    }
}
