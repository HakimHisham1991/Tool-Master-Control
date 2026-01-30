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
        string? consumableCodeFilter,
        string? supplierFilter,
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

        if (!string.IsNullOrWhiteSpace(consumableCodeFilter))
            query = query.Where(t => t.ConsumableCode == consumableCodeFilter);

        if (!string.IsNullOrWhiteSpace(supplierFilter))
            query = query.Where(t => t.Supplier == supplierFilter);

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

        var all = await _context.ToolCodeUniques.AsNoTracking().ToListAsync();

        return new ToolCodeUniqueListViewModel
        {
            Tools = items,
            SearchTerm = searchTerm,
            ConsumableCodeFilter = consumableCodeFilter,
            SupplierFilter = supplierFilter,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize,
            AvailableConsumableCodes = all.Select(t => t.ConsumableCode).Distinct().OrderBy(x => x).ToList(),
            AvailableSuppliers = all.Select(t => t.Supplier).Distinct().OrderBy(x => x).ToList()
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
