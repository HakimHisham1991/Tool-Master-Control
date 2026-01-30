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
    public async Task<IActionResult> UserManagement(string? search, int page = 1, int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
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
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
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
    
    [HttpPost]
    public IActionResult ResetUser()
    {
        try
        {
            DbSeeder.ResetUsers(_context);
            return Json(new { success = true, message = "Users reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // Project Code Management
    public async Task<IActionResult> ProjectCode(string? search, int page = 1, int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.ProjectCodes.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p => 
                p.Code.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.Project != null && p.Project.ToLower().Contains(term)));
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
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
        return View(projectCodes);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateProjectCode(string code, string? description, string? project)
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
            Project = project,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.ProjectCodes.Add(projectCode);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Project code created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateProjectCode(int id, string? description, string? project, bool? isActive)
    {
        var projectCode = await _context.ProjectCodes.FindAsync(id);
        if (projectCode == null)
        {
            return Json(new { success = false, message = "Project code not found" });
        }
        
        if (description != null) projectCode.Description = description;
        projectCode.Project = string.IsNullOrEmpty(project) ? null : project;
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
    
    [HttpPost]
    public IActionResult ResetProjectCode()
    {
        try
        {
            DbSeeder.ResetProjectCodes(_context);
            return Json(new { success = true, message = "Project codes reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // Machine Name Management
    public async Task<IActionResult> MachineName(string? search, int page = 1, int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.MachineNames.Include(m => m.MachineModel).AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(m => 
                m.Name.ToLower().Contains(term) ||
                (m.Description != null && m.Description.ToLower().Contains(term)) ||
                (m.Workcenter != null && m.Workcenter.ToLower().Contains(term)));
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
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
        return View(machineNames);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMachineWorkcenters()
    {
        var list = await _context.MachineWorkcenters
            .Where(w => w.IsActive)
            .OrderBy(w => w.Workcenter)
            .Select(w => new { value = w.Workcenter, text = w.Workcenter })
            .ToListAsync();
        return Json(list);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMachineModels()
    {
        var list = await _context.MachineModels
            .Where(m => m.IsActive)
            .OrderBy(m => m.Model)
            .Select(m => new { value = m.Id.ToString(), text = m.Model })
            .ToListAsync();
        return Json(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMachineName(string name, string? description, string? workcenter, int? machineModelId)
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
            Workcenter = workcenter ?? "",
            MachineModelId = machineModelId > 0 ? machineModelId : null,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.MachineNames.Add(machineName);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine name created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMachineName(int id, string? description, string? workcenter, int? machineModelId, bool? isActive)
    {
        var machineName = await _context.MachineNames.FindAsync(id);
        if (machineName == null)
        {
            return Json(new { success = false, message = "Machine name not found" });
        }
        
        if (description != null) machineName.Description = description;
        if (workcenter != null) machineName.Workcenter = workcenter;
        if (machineModelId.HasValue) machineName.MachineModelId = machineModelId > 0 ? machineModelId : null;
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
    
    [HttpPost]
    public IActionResult ResetMachineName()
    {
        try
        {
            DbSeeder.ResetMachineNames(_context);
            return Json(new { success = true, message = "Machine names reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // Machine Workcenter Management
    public async Task<IActionResult> MachineWorkcenter(string? search, int page = 1, int pageSize = 250)
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
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
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
    
    [HttpPost]
    public IActionResult ResetMachineWorkcenter()
    {
        try
        {
            DbSeeder.ResetMachineWorkcenters(_context);
            return Json(new { success = true, message = "Machine workcenters reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // Machine Model Management
    public async Task<IActionResult> MachineModel(string? search, int page = 1, int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.MachineModels.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(m => 
                m.Model.ToLower().Contains(term) ||
                (m.Description != null && m.Description.ToLower().Contains(term)) ||
                (m.Type != null && m.Type.ToLower().Contains(term)) ||
                (m.Controller != null && m.Controller.ToLower().Contains(term)));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var models = await query
            .OrderBy(m => m.Model)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        
        return View(models);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMachineModel(string model, string? machineBuilder, string? type, string? controller)
    {
        if (string.IsNullOrWhiteSpace(model))
            return Json(new { success = false, message = "Model is required" });
        
        if (await _context.MachineModels.AnyAsync(m => m.Model == model))
            return Json(new { success = false, message = "Machine model already exists" });
        
        _context.MachineModels.Add(new MachineModel
        {
            Model = model,
            Description = string.IsNullOrWhiteSpace(machineBuilder) ? null : machineBuilder,
            Type = string.IsNullOrWhiteSpace(type) ? null : type,
            Controller = string.IsNullOrWhiteSpace(controller) ? null : controller,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Machine model created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMachineModel(int id, string? machineBuilder, string? type, string? controller, bool? isActive)
    {
        var machineModel = await _context.MachineModels.FindAsync(id);
        if (machineModel == null)
            return Json(new { success = false, message = "Machine model not found" });
        
        machineModel.Description = string.IsNullOrWhiteSpace(machineBuilder) ? null : machineBuilder;
        machineModel.Type = string.IsNullOrWhiteSpace(type) ? null : type;
        machineModel.Controller = string.IsNullOrWhiteSpace(controller) ? null : controller;
        if (isActive.HasValue) machineModel.IsActive = isActive.Value;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Machine model updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteMachineModel(int id)
    {
        var machineModel = await _context.MachineModels.FindAsync(id);
        if (machineModel == null)
        {
            return Json(new { success = false, message = "Machine model not found" });
        }
        
        _context.MachineModels.Remove(machineModel);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine model deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetMachineModel()
    {
        try
        {
            DbSeeder.ResetMachineModels(_context);
            return Json(new { success = true, message = "Machine models reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // CAM Leader Management
    public async Task<IActionResult> CamLeader(string? search, int page = 1, int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.CamLeaders.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var list = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
        return View(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateCamLeader(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required" });
        
        if (await _context.CamLeaders.AnyAsync(c => c.Name == name))
            return Json(new { success = false, message = "CAM leader already exists" });
        
        _context.CamLeaders.Add(new CamLeader
        {
            Name = name,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "CAM leader created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateCamLeader(int id, string? description, bool? isActive)
    {
        var item = await _context.CamLeaders.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "CAM leader not found" });
        
        if (description != null) item.Description = description;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "CAM leader updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteCamLeader(int id)
    {
        var item = await _context.CamLeaders.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "CAM leader not found" });
        
        _context.CamLeaders.Remove(item);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "CAM leader deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetCamLeader()
    {
        try
        {
            DbSeeder.ResetCamLeaders(_context);
            return Json(new { success = true, message = "CAM leaders reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // CAM Programmer Management
    public async Task<IActionResult> CamProgrammer(string? search, int page = 1, int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.CamProgrammers.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var list = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
        return View(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateCamProgrammer(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required" });
        
        if (await _context.CamProgrammers.AnyAsync(c => c.Name == name))
            return Json(new { success = false, message = "CAM programmer already exists" });
        
        _context.CamProgrammers.Add(new CamProgrammer
        {
            Name = name,
            Description = description,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "CAM programmer created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateCamProgrammer(int id, string? description, bool? isActive)
    {
        var item = await _context.CamProgrammers.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "CAM programmer not found" });
        
        if (description != null) item.Description = description;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "CAM programmer updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteCamProgrammer(int id)
    {
        var item = await _context.CamProgrammers.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "CAM programmer not found" });
        
        _context.CamProgrammers.Remove(item);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "CAM programmer deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetCamProgrammer()
    {
        try
        {
            DbSeeder.ResetCamProgrammers(_context);
            return Json(new { success = true, message = "CAM programmers reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProjectCodesForPartNumberDropdown()
    {
        var list = await _context.ProjectCodes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .Select(p => new { id = p.Id, code = p.Code, project = p.Project ?? "", customer = p.Description ?? "" })
            .ToListAsync();
        return Json(list);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMaterialSpecsForPartNumberDropdown()
    {
        var list = await _context.MaterialSpecs
            .Where(m => m.IsActive)
            .OrderBy(m => m.Spec)
            .ThenBy(m => m.Material)
            .Select(m => new { id = m.Id, spec = m.Spec, material = m.Material })
            .ToListAsync();
        return Json(list);
    }
    
    // Part Number Management
    public async Task<IActionResult> PartNumber(string? search, int page = 1, int pageSize = 250)
    {
        var query = _context.PartNumbers
            .Include(p => p.ProjectCode)
            .Include(p => p.MaterialSpec)
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.PartRev != null && p.PartRev.ToLower().Contains(term)) ||
                (p.DrawingRev != null && p.DrawingRev.ToLower().Contains(term)) ||
                (p.RefDrawing != null && p.RefDrawing.ToLower().Contains(term)) ||
                (p.ProjectCode != null && (p.ProjectCode.Code.ToLower().Contains(term) || (p.ProjectCode.Project != null && p.ProjectCode.Project.ToLower().Contains(term)) || (p.ProjectCode.Description != null && p.ProjectCode.Description.ToLower().Contains(term)))) ||
                (p.MaterialSpec != null && (p.MaterialSpec.Spec.ToLower().Contains(term) || p.MaterialSpec.Material.ToLower().Contains(term))));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var list = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
        var projectCodes = await _context.ProjectCodes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .Select(p => new { id = p.Id, code = p.Code, project = p.Project ?? "", customer = p.Description ?? "" })
            .ToListAsync();
        var materialSpecs = await _context.MaterialSpecs
            .Where(m => m.IsActive)
            .OrderBy(m => m.Spec)
            .ThenBy(m => m.Material)
            .Select(m => new { id = m.Id, spec = m.Spec, material = m.Material })
            .ToListAsync();
        ViewBag.ProjectCodesForPartNumber = projectCodes;
        ViewBag.MaterialSpecsForPartNumber = materialSpecs;
        
        return View(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePartNumber(string name, string? description, int? projectCodeId, int? materialSpecId, string? partRev, string? drawingRev, string? refDrawing)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Part number is required" });
        
        if (await _context.PartNumbers.AnyAsync(p => p.Name == name))
            return Json(new { success = false, message = "Part number already exists" });
        
        _context.PartNumbers.Add(new PartNumber
        {
            Name = name,
            Description = description,
            ProjectCodeId = projectCodeId > 0 ? projectCodeId : null,
            MaterialSpecId = materialSpecId > 0 ? materialSpecId : null,
            PartRev = partRev,
            DrawingRev = drawingRev,
            RefDrawing = refDrawing,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Part number created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdatePartNumber(int id, string? description, int? projectCodeId, int? materialSpecId, string? partRev, string? drawingRev, string? refDrawing, bool? isActive)
    {
        var item = await _context.PartNumbers.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "Part number not found" });
        
        if (description != null) item.Description = description;
        item.ProjectCodeId = projectCodeId > 0 ? projectCodeId : null;
        item.MaterialSpecId = materialSpecId > 0 ? materialSpecId : null;
        item.PartRev = string.IsNullOrWhiteSpace(partRev) ? null : partRev;
        item.DrawingRev = string.IsNullOrWhiteSpace(drawingRev) ? null : drawingRev;
        item.RefDrawing = string.IsNullOrWhiteSpace(refDrawing) ? null : refDrawing;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Part number updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeletePartNumber(int id)
    {
        var item = await _context.PartNumbers.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "Part number not found" });
        
        _context.PartNumbers.Remove(item);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Part number deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetPartNumber()
    {
        try
        {
            DbSeeder.ResetPartNumbers(_context);
            return Json(new { success = true, message = "Part numbers reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // Material Spec. Management
    public async Task<IActionResult> MaterialSpec(string? search, int page = 1, int pageSize = 250)
    {
        var query = _context.MaterialSpecs.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(m => 
                m.Spec.ToLower().Contains(term) ||
                m.Material.ToLower().Contains(term));
        }
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var items = await query
            .OrderBy(m => m.Spec)
            .ThenBy(m => m.Material)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.PaginationQuery = string.IsNullOrEmpty(search) ? "" : "search=" + Uri.EscapeDataString(search);
        
        return View(items);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMaterialSpec(string spec, string material)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return Json(new { success = false, message = "Material Spec. is required" });
        }
        if (string.IsNullOrWhiteSpace(material))
        {
            return Json(new { success = false, message = "Material is required" });
        }
        
        if (await _context.MaterialSpecs.AnyAsync(m => m.Spec == spec && m.Material == material))
        {
            return Json(new { success = false, message = "This Spec / Material pair already exists" });
        }
        
        var item = new MaterialSpec
        {
            Spec = spec,
            Material = material,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.MaterialSpecs.Add(item);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Material Spec. created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMaterialSpec(int id, string? material, bool? isActive)
    {
        var item = await _context.MaterialSpecs.FindAsync(id);
        if (item == null)
        {
            return Json(new { success = false, message = "Material Spec. not found" });
        }
        
        if (material != null) item.Material = material;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Material Spec. updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteMaterialSpec(int id)
    {
        var item = await _context.MaterialSpecs.FindAsync(id);
        if (item == null)
        {
            return Json(new { success = false, message = "Material Spec. not found" });
        }
        
        _context.MaterialSpecs.Remove(item);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Material Spec. deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetMaterialSpec()
    {
        try
        {
            DbSeeder.ResetMaterialSpecs(_context);
            return Json(new { success = true, message = "Material specs reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
