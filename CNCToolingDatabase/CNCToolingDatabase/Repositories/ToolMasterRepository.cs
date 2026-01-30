using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Repositories;

public class ToolMasterRepository : IToolMasterRepository
{
    private readonly ApplicationDbContext _context;
    
    public ToolMasterRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ToolMaster>> GetAllAsync()
    {
        return await _context.ToolMasters.OrderBy(t => t.ConsumableCode).ToListAsync();
    }
    
    public async Task<ToolMaster?> GetByConsumableCodeAsync(string consumableCode)
    {
        return await _context.ToolMasters
            .FirstOrDefaultAsync(t => t.ConsumableCode == consumableCode);
    }
    
    public async Task UpdateOrCreateAsync(ToolMaster toolMaster)
    {
        var existing = await GetByConsumableCodeAsync(toolMaster.ConsumableCode);
        if (existing == null)
        {
            _context.ToolMasters.Add(toolMaster);
        }
        else
        {
            existing.ToolDescription = toolMaster.ToolDescription;
            existing.Supplier = toolMaster.Supplier;
            existing.Diameter = toolMaster.Diameter;
            existing.FluteLength = toolMaster.FluteLength;
            existing.ProtrusionLength = toolMaster.ProtrusionLength;
            existing.CornerRadius = toolMaster.CornerRadius;
            existing.HolderExtensionCode = toolMaster.HolderExtensionCode;
            existing.ArborCode = toolMaster.ArborCode;
            existing.LastUpdated = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateFromDetailAsync(ToolListDetail detail)
    {
        if (string.IsNullOrWhiteSpace(detail.ConsumableCode))
            return;
            
        var toolMaster = new ToolMaster
        {
            ConsumableCode = detail.ConsumableCode,
            ToolDescription = detail.ToolDescription,
            Supplier = detail.Supplier,
            Diameter = detail.Diameter,
            FluteLength = detail.FluteLength,
            ProtrusionLength = detail.ProtrusionLength,
            CornerRadius = detail.CornerRadius,
            HolderExtensionCode = detail.HolderExtensionCode,
            ArborCode = detail.ArborCode
        };
        
        await UpdateOrCreateAsync(toolMaster);
    }
}
