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
        if (diameterVal.HasValue)
            query = query.Where(t => t.Diameter == diameterVal.Value);
        if (fluteLengthVal.HasValue)
            query = query.Where(t => t.FluteLength == fluteLengthVal.Value);
        if (protrusionLengthVal.HasValue)
            query = query.Where(t => t.ProtrusionLength == protrusionLengthVal.Value);
        if (cornerRadiusVal.HasValue)
            query = query.Where(t => t.CornerRadius == cornerRadiusVal.Value);
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
        
        // Bidirectional filter options: each dropdown shows only values that exist given ALL OTHER filters
        var baseQuery = from detail in _context.ToolListDetails
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
            baseQuery = baseQuery.Where(t =>
                t.ToolNumber.ToLower().Contains(term) ||
                t.ToolDescription.ToLower().Contains(term) ||
                t.ConsumableCode.ToLower().Contains(term) ||
                t.Supplier.ToLower().Contains(term) ||
                t.PartNumber.ToLower().Contains(term));
        }

        IQueryable<ToolCodeViewModel> ApplyFiltersExcept(IQueryable<ToolCodeViewModel> q, string? skip)
        {
            if (skip != "toolNumber" && !string.IsNullOrWhiteSpace(toolNumberFilter)) q = q.Where(t => t.ToolNumber == toolNumberFilter);
            if (skip != "toolDescription" && !string.IsNullOrWhiteSpace(toolDescriptionFilter)) q = q.Where(t => t.ToolDescription != null && t.ToolDescription.Contains(toolDescriptionFilter));
            if (skip != "consumableCode" && !string.IsNullOrWhiteSpace(consumableCodeFilter)) q = q.Where(t => t.ConsumableCode == consumableCodeFilter);
            if (skip != "supplier" && !string.IsNullOrWhiteSpace(supplierFilter)) q = q.Where(t => t.Supplier == supplierFilter);
            if (skip != "holderExtension" && !string.IsNullOrWhiteSpace(holderExtensionFilter)) q = q.Where(t => t.HolderExtensionCode == holderExtensionFilter);
            if (skip != "diameter" && diameterVal.HasValue) q = q.Where(t => t.Diameter == diameterVal.Value);
            if (skip != "fluteLength" && fluteLengthVal.HasValue) q = q.Where(t => t.FluteLength == fluteLengthVal.Value);
            if (skip != "protrusionLength" && protrusionLengthVal.HasValue) q = q.Where(t => t.ProtrusionLength == protrusionLengthVal.Value);
            if (skip != "cornerRadius" && cornerRadiusVal.HasValue) q = q.Where(t => t.CornerRadius == cornerRadiusVal.Value);
            if (skip != "arborCode" && !string.IsNullOrWhiteSpace(arborCodeFilter)) q = q.Where(t => t.ArborCode == arborCodeFilter);
            if (skip != "partNumber" && !string.IsNullOrWhiteSpace(partNumberFilter)) q = q.Where(t => t.PartNumber == partNumberFilter);
            if (skip != "operation" && !string.IsNullOrWhiteSpace(operationFilter)) q = q.Where(t => t.Operation == operationFilter);
            if (skip != "revision" && !string.IsNullOrWhiteSpace(revisionFilter)) q = q.Where(t => t.Revision == revisionFilter);
            if (skip != "toolListName" && !string.IsNullOrWhiteSpace(toolListNameFilter)) q = q.Where(t => t.ToolListName == toolListNameFilter);
            if (skip != "projectCode" && !string.IsNullOrWhiteSpace(projectCodeFilter)) q = q.Where(t => t.ProjectCode == projectCodeFilter);
            if (skip != "machineName" && !string.IsNullOrWhiteSpace(machineNameFilter)) q = q.Where(t => t.MachineName == machineNameFilter);
            if (skip != "machineWorkcenter" && !string.IsNullOrWhiteSpace(machineWorkcenterFilter)) q = q.Where(t => t.MachineWorkcenter == machineWorkcenterFilter);
            if (skip != "createdBy" && !string.IsNullOrWhiteSpace(createdByFilter)) q = q.Where(t => t.CreatedBy == createdByFilter);
            return q;
        }

        var availableToolNumbers = await ApplyFiltersExcept(baseQuery, "toolNumber").Select(t => t.ToolNumber).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableToolDescriptions = await ApplyFiltersExcept(baseQuery, "toolDescription").Select(t => t.ToolDescription).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableConsumableCodes = await ApplyFiltersExcept(baseQuery, "consumableCode").Select(t => t.ConsumableCode).Distinct().OrderBy(x => x).ToListAsync();
        var availableSuppliers = await ApplyFiltersExcept(baseQuery, "supplier").Select(t => t.Supplier).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableHolderExtensions = await ApplyFiltersExcept(baseQuery, "holderExtension").Select(t => t.HolderExtensionCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableDiameters = (await ApplyFiltersExcept(baseQuery, "diameter").Select(t => t.Diameter).Distinct().ToListAsync()).Select(d => d.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableFluteLengths = (await ApplyFiltersExcept(baseQuery, "fluteLength").Select(t => t.FluteLength).Distinct().ToListAsync()).Select(f => f.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableProtrusionLengths = (await ApplyFiltersExcept(baseQuery, "protrusionLength").Select(t => t.ProtrusionLength).Distinct().ToListAsync()).Select(p => p.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableCornerRadii = (await ApplyFiltersExcept(baseQuery, "cornerRadius").Select(t => t.CornerRadius).Distinct().ToListAsync()).Select(c => c.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var availableArborCodes = await ApplyFiltersExcept(baseQuery, "arborCode").Select(t => t.ArborCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availablePartNumbers = await ApplyFiltersExcept(baseQuery, "partNumber").Select(t => t.PartNumber).Distinct().OrderBy(x => x).ToListAsync();
        var availableOperations = await ApplyFiltersExcept(baseQuery, "operation").Select(t => t.Operation).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableRevisions = await ApplyFiltersExcept(baseQuery, "revision").Select(t => t.Revision).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableToolListNames = await ApplyFiltersExcept(baseQuery, "toolListName").Select(t => t.ToolListName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableProjectCodes = await ApplyFiltersExcept(baseQuery, "projectCode").Select(t => t.ProjectCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableMachineNames = await ApplyFiltersExcept(baseQuery, "machineName").Select(t => t.MachineName).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableMachineWorkcenters = await ApplyFiltersExcept(baseQuery, "machineWorkcenter").Select(t => t.MachineWorkcenter).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();
        var availableCreatedBys = await ApplyFiltersExcept(baseQuery, "createdBy").Select(t => t.CreatedBy).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToListAsync();

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
