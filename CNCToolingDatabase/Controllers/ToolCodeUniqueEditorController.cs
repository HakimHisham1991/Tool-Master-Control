using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models;
using CNCToolingDatabase.Models.ViewModels;

namespace CNCToolingDatabase.Controllers;

public class ToolCodeUniqueEditorController : Controller
{
    private readonly ApplicationDbContext _context;

    public ToolCodeUniqueEditorController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            var entity = await _context.ToolCodeUniques.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id.Value);
            if (entity == null)
                return RedirectToAction("Index", "ToolCodeUnique");
            var vm = new ToolCodeUniqueEditorViewModel
            {
                Id = entity.Id,
                SystemToolName = entity.SystemToolName,
                ConsumableCode = entity.ConsumableCode,
                Supplier = entity.Supplier,
                Diameter = entity.Diameter,
                FluteLength = entity.FluteLength,
                CornerRadius = entity.CornerRadius,
                CreatedDate = entity.CreatedDate,
                LastModifiedDate = entity.LastModifiedDate
            };
            return View(vm);
        }
        return View(new ToolCodeUniqueEditorViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveToolCodeUniqueRequest request)
    {
        var code = (request.ConsumableCode ?? "").Trim();
        if (string.IsNullOrEmpty(code))
        {
            return Json(new { success = false, message = "Consumable Tool Description is required.", id = (int?)null });
        }

        // Duplicate check: ConsumableCode must be unique
        var existing = await _context.ToolCodeUniques
            .Where(t => t.ConsumableCode == code)
            .FirstOrDefaultAsync();
        if (existing != null)
        {
            if (!request.Id.HasValue || request.Id.Value != existing.Id)
                return Json(new { success = false, message = "Consumable Tool Description already exists. Duplicates are not allowed.", id = (int?)null });
        }

        if (request.Id.HasValue && request.Id.Value > 0)
        {
            var entity = await _context.ToolCodeUniques.FirstOrDefaultAsync(t => t.Id == request.Id.Value);
            if (entity == null)
                return Json(new { success = false, message = "Tool code not found.", id = (int?)null });
            entity.SystemToolName = (request.SystemToolName ?? "").Trim();
            entity.ConsumableCode = code;
            entity.Supplier = (request.Supplier ?? "").Trim();
            entity.Diameter = request.Diameter;
            entity.FluteLength = request.FluteLength;
            entity.CornerRadius = request.CornerRadius;
            entity.LastModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Saved.", id = entity.Id });
        }

        var created = new ToolCodeUnique
        {
            SystemToolName = (request.SystemToolName ?? "").Trim(),
            ConsumableCode = code,
            Supplier = (request.Supplier ?? "").Trim(),
            Diameter = request.Diameter,
            FluteLength = request.FluteLength,
            CornerRadius = request.CornerRadius,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
        _context.ToolCodeUniques.Add(created);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Saved.", id = created.Id });
    }
}
