using System.Globalization;
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
        // Parse decimal/date filters once to avoid EF Core translation issues with out var in lambdas
        decimal? diameterVal = null;
        decimal? fluteLengthVal = null;
        decimal? cornerRadiusVal = null;
        DateTime? createdDateVal = null;
        if (!string.IsNullOrWhiteSpace(diameterFilter) && decimal.TryParse(diameterFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            diameterVal = d;
        if (!string.IsNullOrWhiteSpace(fluteLengthFilter) && decimal.TryParse(fluteLengthFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
            fluteLengthVal = f;
        if (!string.IsNullOrWhiteSpace(cornerRadiusFilter) && decimal.TryParse(cornerRadiusFilter, NumberStyles.Any, CultureInfo.InvariantCulture, out var c))
            cornerRadiusVal = c;
        if (!string.IsNullOrWhiteSpace(createdDateFilter) && DateTime.TryParse(createdDateFilter, CultureInfo.InvariantCulture, DateTimeStyles.None, out var cd))
            createdDateVal = cd;

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

        if (diameterVal.HasValue)
            query = query.Where(t => t.Diameter == diameterVal.Value);

        if (fluteLengthVal.HasValue)
            query = query.Where(t => t.FluteLength == fluteLengthVal.Value);

        if (cornerRadiusVal.HasValue)
            query = query.Where(t => t.CornerRadius == cornerRadiusVal.Value);

        if (createdDateVal.HasValue)
            query = query.Where(t => t.CreatedDate.Date == createdDateVal.Value.Date);

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

        // Bidirectional filter options: each dropdown shows only values that exist given ALL OTHER filters
        var baseQuery = _context.ToolCodeUniques.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            baseQuery = baseQuery.Where(t =>
                (t.SystemToolName != null && t.SystemToolName.ToLower().Contains(term)) ||
                (t.ConsumableCode != null && t.ConsumableCode.ToLower().Contains(term)) ||
                (t.Supplier != null && t.Supplier.ToLower().Contains(term)));
        }

        IQueryable<ToolCodeUnique> ApplyFiltersExcept(IQueryable<ToolCodeUnique> q, string? skipColumn)
        {
            if (skipColumn != "systemToolName" && !string.IsNullOrWhiteSpace(systemToolNameFilter))
                q = q.Where(t => t.SystemToolName == systemToolNameFilter);
            if (skipColumn != "consumableCode" && !string.IsNullOrWhiteSpace(consumableCodeFilter))
                q = q.Where(t => t.ConsumableCode == consumableCodeFilter);
            if (skipColumn != "supplier" && !string.IsNullOrWhiteSpace(supplierFilter))
                q = q.Where(t => t.Supplier == supplierFilter);
            if (skipColumn != "diameter" && diameterVal.HasValue)
                q = q.Where(t => t.Diameter == diameterVal.Value);
            if (skipColumn != "fluteLength" && fluteLengthVal.HasValue)
                q = q.Where(t => t.FluteLength == fluteLengthVal.Value);
            if (skipColumn != "cornerRadius" && cornerRadiusVal.HasValue)
                q = q.Where(t => t.CornerRadius == cornerRadiusVal.Value);
            if (skipColumn != "createdDate" && createdDateVal.HasValue)
                q = q.Where(t => t.CreatedDate.Date == createdDateVal.Value.Date);
            return q;
        }

        var availableSystemToolNames = await ApplyFiltersExcept(baseQuery, "systemToolName")
            .Select(t => t.SystemToolName).Where(x => x != null && x != "").Distinct().OrderBy(x => x).ToListAsync();

        var availableConsumableCodes = await ApplyFiltersExcept(baseQuery, "consumableCode")
            .Select(t => t.ConsumableCode).Distinct().OrderBy(x => x).ToListAsync();

        var availableSuppliers = await ApplyFiltersExcept(baseQuery, "supplier")
            .Select(t => t.Supplier).Distinct().OrderBy(x => x).ToListAsync();

        var availableDiameters = (await ApplyFiltersExcept(baseQuery, "diameter")
            .Select(t => t.Diameter).Distinct().ToListAsync())
            .Select(d => d.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();

        var availableFluteLengths = (await ApplyFiltersExcept(baseQuery, "fluteLength")
            .Select(t => t.FluteLength).Distinct().ToListAsync())
            .Select(f => f.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();

        var availableCornerRadii = (await ApplyFiltersExcept(baseQuery, "cornerRadius")
            .Select(t => t.CornerRadius).Distinct().ToListAsync())
            .Select(r => r.ToString("0.##")).OrderBy(x => x, StringComparer.Ordinal).ToList();

        var availableCreatedDates = (await ApplyFiltersExcept(baseQuery, "createdDate")
            .Select(t => t.CreatedDate.Date).Distinct().ToListAsync())
            .Select(d => d.ToString("yyyy-MM-dd")).OrderByDescending(x => x).ToList();

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
