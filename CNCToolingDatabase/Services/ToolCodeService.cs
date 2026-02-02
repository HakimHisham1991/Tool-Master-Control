using System.Globalization;
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
        // Parse decimal filters once to avoid EF Core translation issues with out var in lambdas
        decimal? diameterVal = null;
        decimal? fluteLengthVal = null;
        decimal? protrusionLengthVal = null;
        decimal? cornerRadiusVal = null;
        if (!string.IsNullOrWhiteSpace(diameterFilter) && decimal.TryParse(diameterFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            diameterVal = d;
        if (!string.IsNullOrWhiteSpace(fluteLengthFilter) && decimal.TryParse(fluteLengthFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
            fluteLengthVal = f;
        if (!string.IsNullOrWhiteSpace(protrusionLengthFilter) && decimal.TryParse(protrusionLengthFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
            protrusionLengthVal = p;
        if (!string.IsNullOrWhiteSpace(cornerRadiusFilter) && decimal.TryParse(cornerRadiusFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var c))
            cornerRadiusVal = c;

        // Filter on entity (detail/header) in the join so EF Core/SQLite translates decimal comparisons correctly
        var query = from detail in _context.ToolListDetails
                    join header in _context.ToolListHeaders on detail.ToolListHeaderId equals header.Id
                    where (!diameterVal.HasValue || detail.Diameter == diameterVal.Value)
                    where (!fluteLengthVal.HasValue || detail.FluteLength == fluteLengthVal.Value)
                    where (!protrusionLengthVal.HasValue || detail.ProtrusionLength == protrusionLengthVal.Value)
                    where (!cornerRadiusVal.HasValue || detail.CornerRadius == cornerRadiusVal.Value)
                    where (string.IsNullOrWhiteSpace(toolNumberFilter) || detail.ToolNumber == toolNumberFilter)
                    where (string.IsNullOrWhiteSpace(toolDescriptionFilter) || (detail.ToolDescription != null && detail.ToolDescription.Contains(toolDescriptionFilter)))
                    where (string.IsNullOrWhiteSpace(consumableCodeFilter) || detail.ConsumableCode == consumableCodeFilter)
                    where (string.IsNullOrWhiteSpace(supplierFilter) || detail.Supplier == supplierFilter)
                    where (string.IsNullOrWhiteSpace(holderExtensionFilter) || detail.HolderExtensionCode == holderExtensionFilter)
                    where (string.IsNullOrWhiteSpace(arborCodeFilter) || detail.ArborCode == arborCodeFilter)
                    where (string.IsNullOrWhiteSpace(partNumberFilter) || header.PartNumber == partNumberFilter)
                    where (string.IsNullOrWhiteSpace(operationFilter) || header.Operation == operationFilter)
                    where (string.IsNullOrWhiteSpace(revisionFilter) || header.Revision == revisionFilter)
                    where (string.IsNullOrWhiteSpace(toolListNameFilter) || header.ToolListName == toolListNameFilter)
                    where (string.IsNullOrWhiteSpace(projectCodeFilter) || header.ProjectCode == projectCodeFilter)
                    where (string.IsNullOrWhiteSpace(machineNameFilter) || header.MachineName == machineNameFilter)
                    where (string.IsNullOrWhiteSpace(machineWorkcenterFilter) || header.MachineWorkcenter == machineWorkcenterFilter)
                    where (string.IsNullOrWhiteSpace(createdByFilter) || header.CreatedBy == createdByFilter)
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

        query = ApplySort(query, sortColumn, sortDirection);
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var tools = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Bidirectional filter options: build query with entity-based filters, skipping one filter per dropdown
        IQueryable<ToolCodeViewModel> BuildFilteredQuery(string? skipFilter)
        {
            var q = from detail in _context.ToolListDetails
                    join header in _context.ToolListHeaders on detail.ToolListHeaderId equals header.Id
                    where (skipFilter == "diameter" || !diameterVal.HasValue || detail.Diameter == diameterVal.Value)
                    where (skipFilter == "fluteLength" || !fluteLengthVal.HasValue || detail.FluteLength == fluteLengthVal.Value)
                    where (skipFilter == "protrusionLength" || !protrusionLengthVal.HasValue || detail.ProtrusionLength == protrusionLengthVal.Value)
                    where (skipFilter == "cornerRadius" || !cornerRadiusVal.HasValue || detail.CornerRadius == cornerRadiusVal.Value)
                    where (skipFilter == "toolNumber" || string.IsNullOrWhiteSpace(toolNumberFilter) || detail.ToolNumber == toolNumberFilter)
                    where (skipFilter == "toolDescription" || string.IsNullOrWhiteSpace(toolDescriptionFilter) || (detail.ToolDescription != null && detail.ToolDescription.Contains(toolDescriptionFilter)))
                    where (skipFilter == "consumableCode" || string.IsNullOrWhiteSpace(consumableCodeFilter) || detail.ConsumableCode == consumableCodeFilter)
                    where (skipFilter == "supplier" || string.IsNullOrWhiteSpace(supplierFilter) || detail.Supplier == supplierFilter)
                    where (skipFilter == "holderExtension" || string.IsNullOrWhiteSpace(holderExtensionFilter) || detail.HolderExtensionCode == holderExtensionFilter)
                    where (skipFilter == "arborCode" || string.IsNullOrWhiteSpace(arborCodeFilter) || detail.ArborCode == arborCodeFilter)
                    where (skipFilter == "partNumber" || string.IsNullOrWhiteSpace(partNumberFilter) || header.PartNumber == partNumberFilter)
                    where (skipFilter == "operation" || string.IsNullOrWhiteSpace(operationFilter) || header.Operation == operationFilter)
                    where (skipFilter == "revision" || string.IsNullOrWhiteSpace(revisionFilter) || header.Revision == revisionFilter)
                    where (skipFilter == "toolListName" || string.IsNullOrWhiteSpace(toolListNameFilter) || header.ToolListName == toolListNameFilter)
                    where (skipFilter == "projectCode" || string.IsNullOrWhiteSpace(projectCodeFilter) || header.ProjectCode == projectCodeFilter)
                    where (skipFilter == "machineName" || string.IsNullOrWhiteSpace(machineNameFilter) || header.MachineName == machineNameFilter)
                    where (skipFilter == "machineWorkcenter" || string.IsNullOrWhiteSpace(machineWorkcenterFilter) || header.MachineWorkcenter == machineWorkcenterFilter)
                    where (skipFilter == "createdBy" || string.IsNullOrWhiteSpace(createdByFilter) || header.CreatedBy == createdByFilter)
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
                q = q.Where(t =>
                    t.ToolNumber.ToLower().Contains(term) ||
                    t.ToolDescription.ToLower().Contains(term) ||
                    t.ConsumableCode.ToLower().Contains(term) ||
                    t.Supplier.ToLower().Contains(term) ||
                    t.PartNumber.ToLower().Contains(term));
            }
            return q;
        }

        var availableToolNumbers = await BuildFilteredQuery("toolNumber").Select(t => t.ToolNumber).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableToolDescriptions = await BuildFilteredQuery("toolDescription").Select(t => t.ToolDescription).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableConsumableCodes = await BuildFilteredQuery("consumableCode").Select(t => t.ConsumableCode).Distinct().OrderBy(x => x).ToListAsync();
        var availableSuppliers = await BuildFilteredQuery("supplier").Select(t => t.Supplier).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableHolderExtensions = await BuildFilteredQuery("holderExtension").Select(t => t.HolderExtensionCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableDiameters = (await BuildFilteredQuery("diameter").Select(t => t.Diameter).Distinct().ToListAsync()).Select(d => d.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableFluteLengths = (await BuildFilteredQuery("fluteLength").Select(t => t.FluteLength).Distinct().ToListAsync()).Select(f => f.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableProtrusionLengths = (await BuildFilteredQuery("protrusionLength").Select(t => t.ProtrusionLength).Distinct().ToListAsync()).Select(p => p.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableCornerRadii = (await BuildFilteredQuery("cornerRadius").Select(t => t.CornerRadius).Distinct().ToListAsync()).Select(c => c.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableArborCodes = await BuildFilteredQuery("arborCode").Select(t => t.ArborCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availablePartNumbers = await BuildFilteredQuery("partNumber").Select(t => t.PartNumber).Distinct().OrderBy(x => x).ToListAsync();
        var availableOperations = await BuildFilteredQuery("operation").Select(t => t.Operation).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableRevisions = await BuildFilteredQuery("revision").Select(t => t.Revision).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableToolListNames = await BuildFilteredQuery("toolListName").Select(t => t.ToolListName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableProjectCodes = await BuildFilteredQuery("projectCode").Select(t => t.ProjectCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableMachineNames = await BuildFilteredQuery("machineName").Select(t => t.MachineName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableMachineWorkcenters = await BuildFilteredQuery("machineWorkcenter").Select(t => t.MachineWorkcenter).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableCreatedBys = await BuildFilteredQuery("createdBy").Select(t => t.CreatedBy).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();

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
            AvailableToolNumbers = availableToolNumbers,
            AvailableToolDescriptions = availableToolDescriptions,
            AvailableConsumableCodes = availableConsumableCodes,
            AvailableSuppliers = availableSuppliers,
            AvailableHolderExtensions = availableHolderExtensions,
            AvailableDiameters = availableDiameters,
            AvailableFluteLengths = availableFluteLengths,
            AvailableProtrusionLengths = availableProtrusionLengths,
            AvailableCornerRadii = availableCornerRadii,
            AvailableArborCodes = availableArborCodes,
            AvailablePartNumbers = availablePartNumbers,
            AvailableOperations = availableOperations,
            AvailableRevisions = availableRevisions,
            AvailableToolListNames = availableToolListNames,
            AvailableProjectCodes = availableProjectCodes,
            AvailableMachineNames = availableMachineNames,
            AvailableMachineWorkcenters = availableMachineWorkcenters,
            AvailableCreatedBys = availableCreatedBys
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
