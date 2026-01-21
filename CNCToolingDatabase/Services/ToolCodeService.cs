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
        string? consumableCodeFilter,
        string? diameterFilter,
        string? arborCodeFilter,
        string? holderExtensionFilter,
        string? partNumberFilter,
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
                        ProjectCode = header.ProjectCode,
                        MachineName = header.MachineName,
                        MachineWorkcenter = header.MachineWorkcenter
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
        
        if (!string.IsNullOrWhiteSpace(consumableCodeFilter))
            query = query.Where(t => t.ConsumableCode == consumableCodeFilter);
        
        if (!string.IsNullOrWhiteSpace(diameterFilter) && decimal.TryParse(diameterFilter, out var dia))
            query = query.Where(t => t.Diameter == dia);
        
        if (!string.IsNullOrWhiteSpace(arborCodeFilter))
            query = query.Where(t => t.ArborCode == arborCodeFilter);
        
        if (!string.IsNullOrWhiteSpace(holderExtensionFilter))
            query = query.Where(t => t.HolderExtensionCode == holderExtensionFilter);
        
        if (!string.IsNullOrWhiteSpace(partNumberFilter))
            query = query.Where(t => t.PartNumber == partNumberFilter);
        
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
            ConsumableCodeFilter = consumableCodeFilter,
            DiameterFilter = diameterFilter,
            ArborCodeFilter = arborCodeFilter,
            HolderExtensionFilter = holderExtensionFilter,
            PartNumberFilter = partNumberFilter,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize,
            AvailableConsumableCodes = allDetails.Select(d => d.ConsumableCode).Distinct().OrderBy(x => x).ToList(),
            AvailableDiameters = allDetails.Select(d => d.Diameter.ToString("0.##")).Distinct().OrderBy(x => x).ToList(),
            AvailableArborCodes = allDetails.Select(d => d.ArborCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailableHolderExtensions = allDetails.Select(d => d.HolderExtensionCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
            AvailablePartNumbers = allHeaders.Select(h => h.PartNumber).Distinct().OrderBy(x => x).ToList()
        };
    }
    
    private IQueryable<ToolCodeViewModel> ApplySort(IQueryable<ToolCodeViewModel> query, string? column, string? direction)
    {
        var isDescending = direction?.ToLower() == "desc";
        
        return column?.ToLower() switch
        {
            "toolnumber" => isDescending ? query.OrderByDescending(t => t.ToolNumber) : query.OrderBy(t => t.ToolNumber),
            "tooldescription" => isDescending ? query.OrderByDescending(t => t.ToolDescription) : query.OrderBy(t => t.ToolDescription),
            "consumablecode" => isDescending ? query.OrderByDescending(t => t.ConsumableCode) : query.OrderBy(t => t.ConsumableCode),
            "supplier" => isDescending ? query.OrderByDescending(t => t.Supplier) : query.OrderBy(t => t.Supplier),
            "diameter" => isDescending ? query.OrderByDescending(t => t.Diameter) : query.OrderBy(t => t.Diameter),
            "partnumber" => isDescending ? query.OrderByDescending(t => t.PartNumber) : query.OrderBy(t => t.PartNumber),
            "operation" => isDescending ? query.OrderByDescending(t => t.Operation) : query.OrderBy(t => t.Operation),
            _ => query.OrderBy(t => t.PartNumber).ThenBy(t => t.Operation).ThenBy(t => t.ToolNumber)
        };
    }
}
