using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models;
using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Services;

public class ToolCodeUniqueService : IToolCodeUniqueService
{
    private readonly ApplicationDbContext _context;

    public ToolCodeUniqueService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ToolCodeUniqueListViewModel> GetToolCodesAsync(
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
        int pageSize)
    {
        var query = _context.ToolCodeUniques.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(t =>
                (t.SystemToolName != null && t.SystemToolName.ToLower().Contains(term)) ||
                (t.ConsumableCode != null && t.ConsumableCode.ToLower().Contains(term)) ||
                (t.Supplier != null && t.Supplier.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(systemToolNameFilter))
            query = query.Where(t => t.SystemToolName == systemToolNameFilter);

        if (!string.IsNullOrWhiteSpace(consumableCodeFilter))
            query = query.Where(t => t.ConsumableCode == consumableCodeFilter);

        if (!string.IsNullOrWhiteSpace(supplierFilter))
            query = query.Where(t => t.Supplier == supplierFilter);

        if (!string.IsNullOrWhiteSpace(diameterFilter) && decimal.TryParse(diameterFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var diameterVal))
            query = query.Where(t => t.Diameter == diameterVal);

        if (!string.IsNullOrWhiteSpace(fluteLengthFilter) && decimal.TryParse(fluteLengthFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fluteVal))
            query = query.Where(t => t.FluteLength == fluteVal);

        if (!string.IsNullOrWhiteSpace(cornerRadiusFilter) && decimal.TryParse(cornerRadiusFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var radiusVal))
            query = query.Where(t => t.CornerRadius == radiusVal);

        if (!string.IsNullOrWhiteSpace(createdDateFilter) && DateTime.TryParse(createdDateFilter, out var createdDateVal))
            query = query.Where(t => t.CreatedDate.Date == createdDateVal.Date);

        query = ApplySort(query, sortColumn, sortDirection);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ToolCodeUniqueItemViewModel
            {
                No = t.Id,
                SystemToolName = t.SystemToolName,
                ConsumableCode = t.ConsumableCode,
                Supplier = t.Supplier,
                Diameter = t.Diameter,
                FluteLength = t.FluteLength,
                CornerRadius = t.CornerRadius,
                CreatedDate = t.CreatedDate,
                LastModifiedDate = t.LastModifiedDate
            })
            .ToListAsync();

        // Cascading filter options: each dropdown shows only values that exist given filters to its left
        var baseQuery = _context.ToolCodeUniques.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            baseQuery = baseQuery.Where(t =>
                (t.SystemToolName != null && t.SystemToolName.ToLower().Contains(term)) ||
                (t.ConsumableCode != null && t.ConsumableCode.ToLower().Contains(term)) ||
                (t.Supplier != null && t.Supplier.ToLower().Contains(term)));
        }

        // Column 2: System Tool Name - from unfiltered base
        var qSystemTool = baseQuery;
        var availableSystemToolNames = await qSystemTool
            .Select(t => t.SystemToolName)
            .Where(x => x != null && x != "")
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        // Column 3: Consumable Tool Description - filtered by System Tool Name
        var qConsumable = qSystemTool;
        if (!string.IsNullOrWhiteSpace(systemToolNameFilter))
            qConsumable = qConsumable.Where(t => t.SystemToolName == systemToolNameFilter);
        var availableConsumableCodes = await qConsumable
            .Select(t => t.ConsumableCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        // Column 4: Supplier - filtered by System Tool Name + Consumable Code
        var qSupplier = qConsumable;
        if (!string.IsNullOrWhiteSpace(consumableCodeFilter))
            qSupplier = qSupplier.Where(t => t.ConsumableCode == consumableCodeFilter);
        var availableSuppliers = await qSupplier
            .Select(t => t.Supplier)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        // Column 5: Diameter - filtered by previous columns (client eval for ToString)
        var qDiameter = qSupplier;
        if (!string.IsNullOrWhiteSpace(supplierFilter))
            qDiameter = qDiameter.Where(t => t.Supplier == supplierFilter);
        var availableDiameters = (await qDiameter.Select(t => t.Diameter).Distinct().ToListAsync())
            .Select(d => d.ToString("0.##"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        // Column 6: Flute Length - filtered by previous columns (client eval for ToString)
        var qFluteLength = qDiameter;
        if (!string.IsNullOrWhiteSpace(diameterFilter) && decimal.TryParse(diameterFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var diaVal))
            qFluteLength = qFluteLength.Where(t => t.Diameter == diaVal);
        var availableFluteLengths = (await qFluteLength.Select(t => t.FluteLength).Distinct().ToListAsync())
            .Select(f => f.ToString("0.##"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        // Column 7: Corner Radius - filtered by previous columns (client eval for ToString)
        var qCornerRadius = qFluteLength;
        if (!string.IsNullOrWhiteSpace(fluteLengthFilter) && decimal.TryParse(fluteLengthFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var flVal))
            qCornerRadius = qCornerRadius.Where(t => t.FluteLength == flVal);
        var availableCornerRadii = (await qCornerRadius.Select(t => t.CornerRadius).Distinct().ToListAsync())
            .Select(r => r.ToString("0.##"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        // Column 8: Created Date - filtered by previous columns (client eval for ToString)
        var qCreatedDate = qCornerRadius;
        if (!string.IsNullOrWhiteSpace(cornerRadiusFilter) && decimal.TryParse(cornerRadiusFilter, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var crVal))
            qCreatedDate = qCreatedDate.Where(t => t.CornerRadius == crVal);
        var availableCreatedDates = (await qCreatedDate.Select(t => t.CreatedDate.Date).Distinct().ToListAsync())
            .Select(d => d.ToString("yyyy-MM-dd"))
            .OrderByDescending(x => x)
            .ToList();

        return new ToolCodeUniqueListViewModel
        {
            Tools = items,
            SearchTerm = searchTerm,
            SystemToolNameFilter = systemToolNameFilter,
            ConsumableCodeFilter = consumableCodeFilter,
            SupplierFilter = supplierFilter,
            DiameterFilter = diameterFilter,
            FluteLengthFilter = fluteLengthFilter,
            CornerRadiusFilter = cornerRadiusFilter,
            CreatedDateFilter = createdDateFilter,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize,
            AvailableSystemToolNames = availableSystemToolNames,
            AvailableConsumableCodes = availableConsumableCodes,
            AvailableSuppliers = availableSuppliers,
            AvailableDiameters = availableDiameters,
            AvailableFluteLengths = availableFluteLengths,
            AvailableCornerRadii = availableCornerRadii,
            AvailableCreatedDates = availableCreatedDates
        };
    }

    private static IQueryable<ToolCodeUnique> ApplySort(IQueryable<ToolCodeUnique> query, string? column, string? direction)
    {
        var isDescending = direction?.ToLower() == "desc";

        return (column?.ToLower()) switch
        {
            "no" => isDescending ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
            "systemtoolname" => isDescending ? query.OrderByDescending(t => t.SystemToolName) : query.OrderBy(t => t.SystemToolName),
            "consumablecode" => isDescending ? query.OrderByDescending(t => t.ConsumableCode) : query.OrderBy(t => t.ConsumableCode),
            "supplier" => isDescending ? query.OrderByDescending(t => t.Supplier) : query.OrderBy(t => t.Supplier),
            "diameter" => isDescending ? query.OrderByDescending(t => t.Diameter) : query.OrderBy(t => t.Diameter),
            "flutelength" => isDescending ? query.OrderByDescending(t => t.FluteLength) : query.OrderBy(t => t.FluteLength),
            "cornerradius" => isDescending ? query.OrderByDescending(t => t.CornerRadius) : query.OrderBy(t => t.CornerRadius),
            "createddate" => isDescending ? query.OrderByDescending(t => t.CreatedDate) : query.OrderBy(t => t.CreatedDate),
            "lastmodifieddate" => isDescending ? query.OrderByDescending(t => t.LastModifiedDate) : query.OrderBy(t => t.LastModifiedDate),
            _ => query.OrderBy(t => t.Id)
        };
    }
}
