using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Repositories;

public class ToolListRepository : IToolListRepository
{
    private readonly ApplicationDbContext _context;
    
    public ToolListRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ToolListHeader>> GetAllHeadersAsync()
    {
        return await _context.ToolListHeaders
            .OrderByDescending(h => h.LastModifiedDate)
            .ToListAsync();
    }
    
    public async Task<ToolListHeader?> GetHeaderByIdAsync(int id)
    {
        return await _context.ToolListHeaders.FindAsync(id);
    }
    
    public async Task<ToolListHeader?> GetHeaderWithDetailsAsync(int id)
    {
        return await _context.ToolListHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.Id == id);
    }
    
    public async Task<ToolListHeader?> GetByPartNumberAndOperationAsync(string partNumber, string operation)
    {
        return await _context.ToolListHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.PartNumber == partNumber && h.Operation == operation);
    }
    
    public async Task<ToolListHeader> CreateHeaderAsync(ToolListHeader header)
    {
        header.GenerateToolListName();
        _context.ToolListHeaders.Add(header);
        await _context.SaveChangesAsync();
        return header;
    }
    
    public async Task UpdateHeaderAsync(ToolListHeader header)
    {
        header.GenerateToolListName();
        header.LastModifiedDate = DateTime.UtcNow;
        _context.ToolListHeaders.Update(header);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteHeaderAsync(int id)
    {
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header != null)
        {
            _context.ToolListHeaders.Remove(header);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<bool> AcquireLockAsync(int headerId, string username)
    {
        var header = await _context.ToolListHeaders.FindAsync(headerId);
        if (header == null) return false;
        
        if (header.LockedBy != null && header.LockedBy != username)
        {
            if (header.LastHeartbeat.HasValue && 
                DateTime.UtcNow - header.LastHeartbeat.Value < TimeSpan.FromMinutes(5))
            {
                return false;
            }
        }
        
        header.LockedBy = username;
        header.LockStartTime = DateTime.UtcNow;
        header.LastHeartbeat = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> ReleaseLockAsync(int headerId, string username)
    {
        var header = await _context.ToolListHeaders.FindAsync(headerId);
        if (header == null) return false;
        
        if (header.LockedBy == username || header.LockedBy == null)
        {
            header.LockedBy = null;
            header.LockStartTime = null;
            header.LastHeartbeat = null;
            await _context.SaveChangesAsync();
            return true;
        }
        
        return false;
    }
    
    public async Task UpdateHeartbeatAsync(int headerId, string username)
    {
        var header = await _context.ToolListHeaders.FindAsync(headerId);
        if (header != null && header.LockedBy == username)
        {
            header.LastHeartbeat = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task ReleaseExpiredLocksAsync(TimeSpan timeout)
    {
        var cutoffTime = DateTime.UtcNow - timeout;
        var expiredHeaders = await _context.ToolListHeaders
            .Where(h => h.LockedBy != null && 
                        h.LastHeartbeat.HasValue && 
                        h.LastHeartbeat.Value < cutoffTime)
            .ToListAsync();
            
        foreach (var header in expiredHeaders)
        {
            header.LockedBy = null;
            header.LockStartTime = null;
            header.LastHeartbeat = null;
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task SaveDetailsAsync(int headerId, List<ToolListDetail> details)
    {
        var existingDetails = await _context.ToolListDetails
            .Where(d => d.ToolListHeaderId == headerId)
            .ToListAsync();
        
        _context.ToolListDetails.RemoveRange(existingDetails);
        
        foreach (var detail in details)
        {
            detail.ToolListHeaderId = headerId;
            _context.ToolListDetails.Add(detail);
        }
        
        await _context.SaveChangesAsync();
    }
}
