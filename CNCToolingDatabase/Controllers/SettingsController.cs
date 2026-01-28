using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Controllers;

public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    // User Management
    public async Task<IActionResult> UserManagement(string? search, int page = 1, int pageSize = 50)
    {
        var query = _context.Users.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(term) ||
                u.DisplayName.ToLower().Contains(term));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var users = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        
        return View("User", users);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser(string username, string password, string displayName)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Json(new { success = false, message = "Username and password are required" });
        }
        
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            return Json(new { success = false, message = "Username already exists" });
        }
        
        var user = new User
        {
            Username = username,
            Password = password, // In production, hash this
            DisplayName = displayName ?? username,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "User created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateUser(int id, string? displayName, bool? isActive)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }
        
        if (displayName != null) user.DisplayName = displayName;
        if (isActive.HasValue) user.IsActive = isActive.Value;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "User updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }
        
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "User deleted successfully" });
    }
    
    // Project Code Management
    public async Task<IActionResult> ProjectCode(string? search, int page = 1, int pageSize = 50)
    {
        var query = _context.ProjectCodes.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p => 
                p.Code.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var projectCodes = await query
            .OrderBy(p => p.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        
        return View(projectCodes);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateProjectCode(string code, string? description)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { success = false, message = "Code is required" });
        }
        
        if (await _context.ProjectCodes.AnyAsync(p => p.Code == code))
        {
            return Json(new { success = false, message = "Project code already exists" });
        }
        
        var projectCode = new ProjectCode
        {
            Code = code,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.ProjectCodes.Add(projectCode);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Project code created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateProjectCode(int id, string? description, bool? isActive)
    {
        var projectCode = await _context.ProjectCodes.FindAsync(id);
        if (projectCode == null)
        {
            return Json(new { success = false, message = "Project code not found" });
        }
        
        if (description != null) projectCode.Description = description;
        if (isActive.HasValue) projectCode.IsActive = isActive.Value;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Project code updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteProjectCode(int id)
    {
        var projectCode = await _context.ProjectCodes.FindAsync(id);
        if (projectCode == null)
        {
            return Json(new { success = false, message = "Project code not found" });
        }
        
        _context.ProjectCodes.Remove(projectCode);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Project code deleted successfully" });
    }
    
    // Machine Name Management
    public async Task<IActionResult> MachineName(string? search, int page = 1, int pageSize = 50)
    {
        var query = _context.MachineNames.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(m => 
                m.Name.ToLower().Contains(term) ||
                (m.Description != null && m.Description.ToLower().Contains(term)));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var machineNames = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        
        return View(machineNames);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMachineName(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Json(new { success = false, message = "Name is required" });
        }
        
        if (await _context.MachineNames.AnyAsync(m => m.Name == name))
        {
            return Json(new { success = false, message = "Machine name already exists" });
        }
        
        var machineName = new MachineName
        {
            Name = name,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.MachineNames.Add(machineName);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine name created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMachineName(int id, string? description, bool? isActive)
    {
        var machineName = await _context.MachineNames.FindAsync(id);
        if (machineName == null)
        {
            return Json(new { success = false, message = "Machine name not found" });
        }
        
        if (description != null) machineName.Description = description;
        if (isActive.HasValue) machineName.IsActive = isActive.Value;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine name updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteMachineName(int id)
    {
        var machineName = await _context.MachineNames.FindAsync(id);
        if (machineName == null)
        {
            return Json(new { success = false, message = "Machine name not found" });
        }
        
        _context.MachineNames.Remove(machineName);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine name deleted successfully" });
    }
    
    // Machine Workcenter Management
    public async Task<IActionResult> MachineWorkcenter(string? search, int page = 1, int pageSize = 50)
    {
        var query = _context.MachineWorkcenters.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(w => 
                w.Workcenter.ToLower().Contains(term) ||
                (w.Description != null && w.Description.ToLower().Contains(term)));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var workcenters = await query
            .OrderBy(w => w.Workcenter)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        
        return View(workcenters);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMachineWorkcenter(string workcenter, string? description)
    {
        if (string.IsNullOrWhiteSpace(workcenter))
        {
            return Json(new { success = false, message = "Workcenter is required" });
        }
        
        if (await _context.MachineWorkcenters.AnyAsync(w => w.Workcenter == workcenter))
        {
            return Json(new { success = false, message = "Machine workcenter already exists" });
        }
        
        var machineWorkcenter = new MachineWorkcenter
        {
            Workcenter = workcenter,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.MachineWorkcenters.Add(machineWorkcenter);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine workcenter created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMachineWorkcenter(int id, string? description, bool? isActive)
    {
        var machineWorkcenter = await _context.MachineWorkcenters.FindAsync(id);
        if (machineWorkcenter == null)
        {
            return Json(new { success = false, message = "Machine workcenter not found" });
        }
        
        if (description != null) machineWorkcenter.Description = description;
        if (isActive.HasValue) machineWorkcenter.IsActive = isActive.Value;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine workcenter updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteMachineWorkcenter(int id)
    {
        var machineWorkcenter = await _context.MachineWorkcenters.FindAsync(id);
        if (machineWorkcenter == null)
        {
            return Json(new { success = false, message = "Machine workcenter not found" });
        }
        
        _context.MachineWorkcenters.Remove(machineWorkcenter);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine workcenter deleted successfully" });
    }
}
