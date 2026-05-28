using CNCToolingDatabase.Data;
using CNCToolingDatabase.Helpers;
using CNCToolingDatabase.Models;
using CNCToolingDatabase.Models.PdfLayout;
using Microsoft.EntityFrameworkCore;

namespace CNCToolingDatabase.Services;

public class PdfLayoutService
{
    private readonly ApplicationDbContext _context;

    public PdfLayoutService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PdfLayoutConfig>> GetAllAsync() =>
        await _context.PdfLayouts.OrderByDescending(l => l.IsDefault).ThenBy(l => l.Name).ToListAsync();

    public async Task<PdfLayoutConfig?> GetByIdAsync(int id) =>
        await _context.PdfLayouts.FindAsync(id);

    public async Task<PdfLayoutDocument?> GetDocumentByIdAsync(int id)
    {
        var layout = await GetByIdAsync(id);
        return layout == null ? null : PdfLayoutDocument.Parse(layout.LayoutJson);
    }

    public async Task<PdfLayoutDocument> GetActiveDocumentAsync()
    {
        var active = await _context.PdfLayouts
            .Where(l => l.IsActive && l.IsDefault)
            .OrderByDescending(l => l.UpdatedDate)
            .FirstOrDefaultAsync();

        if (active != null)
        {
            var doc = PdfLayoutDocument.Parse(active.LayoutJson);
            if (doc != null) return doc;
        }

        return DefaultPdfLayoutFactory.Create();
    }

    public async Task<PdfLayoutConfig> CreateAsync(string name, PdfLayoutDocument document, string createdBy, bool isDefault = false)
    {
        if (isDefault)
            await ClearDefaultAsync();

        var layout = new PdfLayoutConfig
        {
            Name = name,
            LayoutJson = document.ToJson(),
            IsDefault = isDefault,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        _context.PdfLayouts.Add(layout);
        await _context.SaveChangesAsync();
        return layout;
    }

    public async Task<PdfLayoutConfig?> UpdateAsync(int id, string name, PdfLayoutDocument document, bool? isDefault = null)
    {
        var layout = await GetByIdAsync(id);
        if (layout == null) return null;

        if (isDefault == true)
            await ClearDefaultAsync(exceptId: id);

        layout.Name = name;
        layout.LayoutJson = document.ToJson();
        layout.UpdatedDate = DateTime.UtcNow;
        if (isDefault.HasValue)
            layout.IsDefault = isDefault.Value;

        await _context.SaveChangesAsync();
        return layout;
    }

    public async Task<PdfLayoutConfig?> DuplicateAsync(int id, string createdBy)
    {
        var source = await GetByIdAsync(id);
        if (source == null) return null;

        var doc = PdfLayoutDocument.Parse(source.LayoutJson) ?? DefaultPdfLayoutFactory.Create();
        return await CreateAsync($"{source.Name} (Copy)", doc, createdBy, isDefault: false);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var layout = await GetByIdAsync(id);
        if (layout == null) return false;
        if (layout.IsDefault && await _context.PdfLayouts.CountAsync() <= 1)
            return false;

        _context.PdfLayouts.Remove(layout);
        await _context.SaveChangesAsync();

        if (layout.IsDefault)
        {
            var next = await _context.PdfLayouts.OrderBy(l => l.Id).FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> SetDefaultAsync(int id)
    {
        var layout = await GetByIdAsync(id);
        if (layout == null) return false;

        await ClearDefaultAsync(exceptId: id);
        layout.IsDefault = true;
        layout.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task EnsureDefaultLayoutAsync()
    {
        var fresh = DefaultPdfLayoutFactory.Create();

        var existing = await _context.PdfLayouts
            .Where(l => l.IsDefault)
            .OrderByDescending(l => l.UpdatedDate)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            var doc = PdfLayoutDocument.Parse(existing.LayoutJson);
            if (doc == null || doc.Version < 3)
            {
                existing.LayoutJson = fresh.ToJson();
                existing.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return;
        }

        if (await _context.PdfLayouts.AnyAsync()) return;
        await CreateAsync(DefaultPdfLayoutFactory.DefaultLayoutName, fresh, "System", isDefault: true);
    }

    private async Task ClearDefaultAsync(int? exceptId = null)
    {
        var defaults = await _context.PdfLayouts.Where(l => l.IsDefault && l.Id != exceptId).ToListAsync();
        foreach (var layout in defaults)
            layout.IsDefault = false;
    }
}
