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
    public async Task<IActionResult> UserManagement(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id),
            "username" => isDesc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "displayname" => isDesc ? query.OrderByDescending(u => u.DisplayName) : query.OrderBy(u => u.DisplayName),
            "createddate" => isDesc ? query.OrderByDescending(u => u.CreatedDate) : query.OrderBy(u => u.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            _ => query.OrderBy(u => u.Username)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
    public async Task<IActionResult> UpdateUser(int id, string? displayName, bool? isActive, string? newPassword)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Json(new { success = false, message = "User not found" });
        }
        
        if (displayName != null) user.DisplayName = displayName;
        if (isActive.HasValue) user.IsActive = isActive.Value;
        if (!string.IsNullOrWhiteSpace(newPassword)) user.Password = newPassword.Trim();
        
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
            return Json(new { success = true, message = "Users reloaded from MASTER - USER.xlsx successfully." });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = msg });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> UserStamp(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user?.Stamp == null || user.Stamp.Length == 0)
            return NotFound();
        var contentType = "image/png";
        if (user.Stamp.Length >= 2)
        {
            if (user.Stamp[0] == 0xFF && user.Stamp[1] == 0xD8) contentType = "image/jpeg";
            else if (user.Stamp[0] == 0x89 && user.Stamp[1] == 0x50) contentType = "image/png";
            else if (user.Stamp[0] == 0x47 && user.Stamp[1] == 0x49) contentType = "image/gif";
            else if (user.Stamp[0] == 0x42 && user.Stamp[1] == 0x4D) contentType = "image/bmp";
            else if (user.Stamp.Length >= 12 && user.Stamp[8] == 0x57 && user.Stamp[9] == 0x45 && user.Stamp[10] == 0x42) contentType = "image/webp";
        }
        return File(user.Stamp, contentType);
    }
    
    private static readonly HashSet<string> AllowedStampContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp"
    };
    
    [HttpPost]
    public async Task<IActionResult> UpdateUserStamp(int id, IFormFile? stamp)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return Json(new { success = false, message = "User not found" });
        if (stamp == null || stamp.Length == 0)
            return Json(new { success = false, message = "Please select an image file (jpg, jpeg, png, gif, bmp, webp)." });
        var contentType = stamp.ContentType ?? "";
        if (!AllowedStampContentTypes.Contains(contentType))
            return Json(new { success = false, message = "Invalid file type. Allowed: jpg, jpeg, png, gif, bmp, webp." });
        const int maxBytes = 2 * 1024 * 1024;
        if (stamp.Length > maxBytes)
            return Json(new { success = false, message = "Image must be 2 MB or smaller." });
        using var ms = new MemoryStream();
        await stamp.CopyToAsync(ms);
        user.Stamp = ms.ToArray();
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Stamp saved successfully." });
    }
    
    // Project Code Management
    public async Task<IActionResult> ProjectCode(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
            "code" => isDesc ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code),
            "description" or "customer" => isDesc ? query.OrderByDescending(p => p.Description ?? "") : query.OrderBy(p => p.Description ?? ""),
            "project" => isDesc ? query.OrderByDescending(p => p.Project ?? "") : query.OrderBy(p => p.Project ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(p => p.CreatedBy) : query.OrderBy(p => p.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(p => p.IsActive) : query.OrderBy(p => p.IsActive),
            _ => query.OrderBy(p => p.Code)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var projectCodes = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
    public async Task<IActionResult> MachineName(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(m => m.Id) : query.OrderBy(m => m.Id),
            "name" => isDesc ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
            "description" or "serialnumber" => isDesc ? query.OrderByDescending(m => m.Description ?? "") : query.OrderBy(m => m.Description ?? ""),
            "workcenter" => isDesc ? query.OrderByDescending(m => m.Workcenter ?? "") : query.OrderBy(m => m.Workcenter ?? ""),
            "machinemodel" => isDesc ? query.OrderByDescending(m => m.MachineModel != null ? m.MachineModel.Model : "") : query.OrderBy(m => m.MachineModel != null ? m.MachineModel.Model : ""),
            "createdby" => isDesc ? query.OrderByDescending(m => m.CreatedBy) : query.OrderBy(m => m.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(m => m.CreatedDate) : query.OrderBy(m => m.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(m => m.IsActive) : query.OrderBy(m => m.IsActive),
            _ => query.OrderBy(m => m.Name)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var machineNames = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
    public async Task<IActionResult> MachineWorkcenter(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.MachineWorkcenters.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(w => 
                w.Workcenter.ToLower().Contains(term) ||
                (w.Axis != null && w.Axis.ToLower().Contains(term)));
        }
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(w => w.Id) : query.OrderBy(w => w.Id),
            "workcenter" => isDesc ? query.OrderByDescending(w => w.Workcenter) : query.OrderBy(w => w.Workcenter),
            "axis" => isDesc ? query.OrderByDescending(w => w.Axis ?? "") : query.OrderBy(w => w.Axis ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(w => w.CreatedBy) : query.OrderBy(w => w.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(w => w.CreatedDate) : query.OrderBy(w => w.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(w => w.IsActive) : query.OrderBy(w => w.IsActive),
            _ => query.OrderBy(w => w.Workcenter)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var workcenters = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
        return View(workcenters);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMachineWorkcenter(string workcenter, string? axis)
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
            Axis = axis,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.MachineWorkcenters.Add(machineWorkcenter);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Machine workcenter created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMachineWorkcenter(int id, string? axis, bool? isActive)
    {
        var machineWorkcenter = await _context.MachineWorkcenters.FindAsync(id);
        if (machineWorkcenter == null)
        {
            return Json(new { success = false, message = "Machine workcenter not found" });
        }
        
        if (axis != null) machineWorkcenter.Axis = axis;
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
    public async Task<IActionResult> MachineModel(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(m => m.Id) : query.OrderBy(m => m.Id),
            "model" => isDesc ? query.OrderByDescending(m => m.Model) : query.OrderBy(m => m.Model),
            "description" or "machinebuilder" => isDesc ? query.OrderByDescending(m => m.Description ?? "") : query.OrderBy(m => m.Description ?? ""),
            "type" => isDesc ? query.OrderByDescending(m => m.Type ?? "") : query.OrderBy(m => m.Type ?? ""),
            "controller" => isDesc ? query.OrderByDescending(m => m.Controller ?? "") : query.OrderBy(m => m.Controller ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(m => m.CreatedBy) : query.OrderBy(m => m.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(m => m.CreatedDate) : query.OrderBy(m => m.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(m => m.IsActive) : query.OrderBy(m => m.IsActive),
            _ => query.OrderBy(m => m.Model)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var models = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
    public async Task<IActionResult> CamLeader(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id),
            "name" => isDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "description" or "position" => isDesc ? query.OrderByDescending(c => c.Description ?? "") : query.OrderBy(c => c.Description ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(c => c.CreatedBy) : query.OrderBy(c => c.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(c => c.CreatedDate) : query.OrderBy(c => c.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
            _ => query.OrderBy(c => c.Name)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
            return Json(new { success = true, message = "CAM leaders reloaded from MASTER - CAM LEADER.xlsx successfully." });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = msg });
        }
    }
    
    // CAM Programmer Management
    public async Task<IActionResult> CamProgrammer(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id),
            "name" => isDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "description" or "location" => isDesc ? query.OrderByDescending(c => c.Description ?? "") : query.OrderBy(c => c.Description ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(c => c.CreatedBy) : query.OrderBy(c => c.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(c => c.CreatedDate) : query.OrderBy(c => c.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
            _ => query.OrderBy(c => c.Name)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
            return Json(new { success = true, message = "CAM programmers reloaded from MASTER - CAM PROGRAMMER.xlsx successfully." });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = msg });
        }
    }
    
    // Operation Management
    public async Task<IActionResult> Operation(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.Operations.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(term) || (o.Description != null && o.Description.ToLower().Contains(term)));
        }
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id),
            "name" => isDesc ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name),
            "description" => isDesc ? query.OrderByDescending(o => o.Description ?? "") : query.OrderBy(o => o.Description ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(o => o.CreatedBy) : query.OrderBy(o => o.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(o => o.CreatedDate) : query.OrderBy(o => o.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(o => o.IsActive) : query.OrderBy(o => o.IsActive),
            _ => query.OrderBy(o => o.Name)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        return View(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateOperation(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required" });
        if (await _context.Operations.AnyAsync(o => o.Name == name))
            return Json(new { success = false, message = "Operation already exists" });
        _context.Operations.Add(new Operation { Name = name, Description = description, CreatedDate = DateTime.UtcNow, CreatedBy = HttpContext.Session.GetString("Username") ?? "", IsActive = true });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Operation created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateOperation(int id, string? description, bool? isActive)
    {
        var item = await _context.Operations.FindAsync(id);
        if (item == null) return Json(new { success = false, message = "Operation not found" });
        if (description != null) item.Description = description;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Operation updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteOperation(int id)
    {
        var item = await _context.Operations.FindAsync(id);
        if (item == null) return Json(new { success = false, message = "Operation not found" });
        _context.Operations.Remove(item);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Operation deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetOperation()
    {
        try { DbSeeder.ResetOperations(_context); return Json(new { success = true, message = "Operations reloaded from MASTER - OPERATION.xlsx successfully." }); }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }
    
    // Revision Management
    public async Task<IActionResult> Revision(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.Revisions.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(term) || (r.Description != null && r.Description.ToLower().Contains(term)));
        }
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id),
            "name" => isDesc ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            "description" => isDesc ? query.OrderByDescending(r => r.Description ?? "") : query.OrderBy(r => r.Description ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(r => r.CreatedBy) : query.OrderBy(r => r.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(r => r.CreatedDate) : query.OrderBy(r => r.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(r => r.IsActive) : query.OrderBy(r => r.IsActive),
            _ => query.OrderBy(r => r.Name)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        return View(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateRevision(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Name is required" });
        if (await _context.Revisions.AnyAsync(r => r.Name == name))
            return Json(new { success = false, message = "Revision already exists" });
        _context.Revisions.Add(new Revision { Name = name, Description = description, CreatedDate = DateTime.UtcNow, CreatedBy = HttpContext.Session.GetString("Username") ?? "", IsActive = true });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Revision created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateRevision(int id, string? description, bool? isActive)
    {
        var item = await _context.Revisions.FindAsync(id);
        if (item == null) return Json(new { success = false, message = "Revision not found" });
        if (description != null) item.Description = description;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Revision updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteRevision(int id)
    {
        var item = await _context.Revisions.FindAsync(id);
        if (item == null) return Json(new { success = false, message = "Revision not found" });
        _context.Revisions.Remove(item);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Revision deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetRevision()
    {
        try { DbSeeder.ResetRevisions(_context); return Json(new { success = true, message = "Revisions reset to seed data successfully." }); }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
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
    public async Task<IActionResult> PartNumber(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
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
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
            "sequence" => isDesc ? query.OrderByDescending(p => p.Sequence) : query.OrderBy(p => p.Sequence),
            "name" or "partnumber" => isDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "description" => isDesc ? query.OrderByDescending(p => p.Description ?? "") : query.OrderBy(p => p.Description ?? ""),
            "partrev" => isDesc ? query.OrderByDescending(p => p.PartRev ?? "") : query.OrderBy(p => p.PartRev ?? ""),
            "drawingrev" => isDesc ? query.OrderByDescending(p => p.DrawingRev ?? "") : query.OrderBy(p => p.DrawingRev ?? ""),
            "projectcode" => isDesc ? query.OrderByDescending(p => p.ProjectCode != null ? p.ProjectCode.Code : "") : query.OrderBy(p => p.ProjectCode != null ? p.ProjectCode.Code : ""),
            "refdrawing" => isDesc ? query.OrderByDescending(p => p.RefDrawing ?? "") : query.OrderBy(p => p.RefDrawing ?? ""),
            "createdby" => isDesc ? query.OrderByDescending(p => p.CreatedBy) : query.OrderBy(p => p.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(p => p.CreatedDate) : query.OrderBy(p => p.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(p => p.IsActive) : query.OrderBy(p => p.IsActive),
            _ => query.OrderBy(p => p.Sequence)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
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
        
        var nextSeq = await _context.PartNumbers.AnyAsync() ? await _context.PartNumbers.MaxAsync(p => p.Sequence) + 1 : 1;
        _context.PartNumbers.Add(new PartNumber
        {
            Name = name,
            Description = description,
            ProjectCodeId = projectCodeId > 0 ? projectCodeId : null,
            MaterialSpecId = materialSpecId > 0 ? materialSpecId : null,
            PartRev = partRev,
            DrawingRev = drawingRev,
            RefDrawing = refDrawing,
            Sequence = nextSeq,
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
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = "Reset failed: " + inner });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    // Material Spec. Management
    public async Task<IActionResult> MaterialSpec(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.MaterialSpecs.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(m =>
                (m.Spec != null && m.Spec.ToLower().Contains(term)) ||
                (m.MaterialSpecPurchased != null && m.MaterialSpecPurchased.ToLower().Contains(term)) ||
                (m.Material != null && m.Material.ToLower().Contains(term)) ||
                (m.MaterialSupplyConditionPurchased != null && m.MaterialSupplyConditionPurchased.ToLower().Contains(term)) ||
                (m.MaterialType != null && m.MaterialType.ToLower().Contains(term)));
        }
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(m => m.Id) : query.OrderBy(m => m.Id),
            "spec" or "materialspec" => isDesc ? query.OrderByDescending(m => m.Spec).ThenByDescending(m => m.Material) : query.OrderBy(m => m.Spec).ThenBy(m => m.Material),
            "specpurchased" => isDesc ? query.OrderByDescending(m => m.MaterialSpecPurchased).ThenBy(m => m.Spec) : query.OrderBy(m => m.MaterialSpecPurchased).ThenBy(m => m.Spec),
            "material" => isDesc ? query.OrderByDescending(m => m.Material).ThenByDescending(m => m.Spec) : query.OrderBy(m => m.Material).ThenBy(m => m.Spec),
            "supplycondition" => isDesc ? query.OrderByDescending(m => m.MaterialSupplyConditionPurchased).ThenBy(m => m.Spec) : query.OrderBy(m => m.MaterialSupplyConditionPurchased).ThenBy(m => m.Spec),
            "materialtype" => isDesc ? query.OrderByDescending(m => m.MaterialType).ThenBy(m => m.Spec) : query.OrderBy(m => m.MaterialType).ThenBy(m => m.Spec),
            "createdby" => isDesc ? query.OrderByDescending(m => m.CreatedBy) : query.OrderBy(m => m.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(m => m.CreatedDate) : query.OrderBy(m => m.CreatedDate),
            "isactive" => isDesc ? query.OrderByDescending(m => m.IsActive) : query.OrderBy(m => m.IsActive),
            _ => query.OrderBy(m => m.Spec).ThenBy(m => m.Material)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
        return View(items);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateMaterialSpec(string spec, string? materialSpecPurchased, string material, string? materialSupplyConditionPurchased, string? materialType)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            return Json(new { success = false, message = "Material Specification (On Drawing) is required" });
        }
        if (string.IsNullOrWhiteSpace(material))
        {
            return Json(new { success = false, message = "General Name is required" });
        }
        
        if (await _context.MaterialSpecs.AnyAsync(m => m.Spec == spec && m.Material == material))
        {
            return Json(new { success = false, message = "This Material Specification (On Drawing) / General Name pair already exists" });
        }
        
        var item = new MaterialSpec
        {
            Spec = spec,
            MaterialSpecPurchased = materialSpecPurchased ?? "",
            Material = material,
            MaterialSupplyConditionPurchased = materialSupplyConditionPurchased ?? "",
            MaterialType = materialType ?? "",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? "",
            IsActive = true
        };
        
        _context.MaterialSpecs.Add(item);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Material Specification (On Drawing) created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateMaterialSpec(int id, string? materialSpecPurchased, string? material, string? materialSupplyConditionPurchased, string? materialType, bool? isActive)
    {
        var item = await _context.MaterialSpecs.FindAsync(id);
        if (item == null)
        {
            return Json(new { success = false, message = "Material Specification (On Drawing) not found" });
        }
        
        if (materialSpecPurchased != null) item.MaterialSpecPurchased = materialSpecPurchased;
        if (material != null) item.Material = material;
        if (materialSupplyConditionPurchased != null) item.MaterialSupplyConditionPurchased = materialSupplyConditionPurchased;
        if (materialType != null) item.MaterialType = materialType;
        if (isActive.HasValue) item.IsActive = isActive.Value;
        
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Material Specification (On Drawing) updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteMaterialSpec(int id)
    {
        var item = await _context.MaterialSpecs.FindAsync(id);
        if (item == null)
        {
            return Json(new { success = false, message = "Material Specification (On Drawing) not found" });
        }
        
        _context.MaterialSpecs.Remove(item);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Material Specification (On Drawing) deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetMaterialSpec()
    {
        try
        {
            DbSeeder.ResetMaterialSpecs(_context);
            return Json(new { success = true, message = "Material specifications reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = msg });
        }
    }
    
    // Tool Supplier Management
    public async Task<IActionResult> ToolSupplier(string? search, int page = 1, int pageSize = 250, string? sortColumn = null, string? sortDirection = null)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var query = _context.ToolSuppliers.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term) ||
                (t.Website != null && t.Website.ToLower().Contains(term)) ||
                (t.Status != null && t.Status.ToLower().Contains(term)));
        }
        
        var isDesc = sortDirection?.ToLower() == "desc";
        query = (sortColumn?.ToLower()) switch
        {
            "id" => isDesc ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
            "name" => isDesc ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "website" => isDesc ? query.OrderByDescending(t => t.Website ?? "") : query.OrderBy(t => t.Website ?? ""),
            "status" => isDesc ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "createdby" => isDesc ? query.OrderByDescending(t => t.CreatedBy) : query.OrderBy(t => t.CreatedBy),
            "createddate" => isDesc ? query.OrderByDescending(t => t.CreatedDate) : query.OrderBy(t => t.CreatedDate),
            _ => query.OrderBy(t => t.Name)
        };
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var list = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.PageSize = pageSize;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortDirection = sortDirection;
        ViewBag.PaginationQuery = BuildPaginationQuery(search, sortColumn, sortDirection);
        
        return View(list);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateToolSupplier(string name, string? website, string status)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Json(new { success = false, message = "Tool Supplier name is required" });
        
        if (await _context.ToolSuppliers.AnyAsync(t => t.Name == name))
            return Json(new { success = false, message = "Tool Supplier already exists" });
        
        _context.ToolSuppliers.Add(new ToolSupplier
        {
            Name = name,
            Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim(),
            Status = status ?? "",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("Username") ?? ""
        });
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Tool Supplier created successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateToolSupplier(int id, string? website, string? status)
    {
        var item = await _context.ToolSuppliers.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "Tool Supplier not found" });
        
        if (website != null) item.Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        if (status != null) item.Status = status;
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Tool Supplier updated successfully" });
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteToolSupplier(int id)
    {
        var item = await _context.ToolSuppliers.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "Tool Supplier not found" });
        
        _context.ToolSuppliers.Remove(item);
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Tool Supplier deleted successfully" });
    }
    
    [HttpPost]
    public IActionResult ResetToolSupplier()
    {
        try
        {
            DbSeeder.ResetToolSuppliers(_context);
            return Json(new { success = true, message = "Tool Suppliers reloaded from MASTER - TOOL SUPPLIER.xlsx successfully." });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = msg });
        }
    }
    
    [HttpPost]
    public IActionResult ResetAllSettings()
    {
        var errors = new List<string>();
        (string Name, Action Reset)[] order =
        {
            ("User", () => { DbSeeder.ResetUsers(_context); }),
            ("Part Number", () => { DbSeeder.ResetPartNumbers(_context); }),
            ("Project Code", () => { DbSeeder.ResetProjectCodes(_context); }),
            ("Machine Name", () => { DbSeeder.ResetMachineNames(_context); }),
            ("Machine Workcenter", () => { DbSeeder.ResetMachineWorkcenters(_context); }),
            ("Machine Model", () => { DbSeeder.ResetMachineModels(_context); }),
            ("Operation", () => { DbSeeder.ResetOperations(_context); }),
            ("Revision", () => { DbSeeder.ResetRevisions(_context); }),
            ("CAM Leader", () => { DbSeeder.ResetCamLeaders(_context); }),
            ("CAM Programmer", () => { DbSeeder.ResetCamProgrammers(_context); }),
            ("Material Specification", () => { DbSeeder.ResetMaterialSpecs(_context); }),
            ("Tool Supplier", () => { DbSeeder.ResetToolSuppliers(_context); }),
        };
        foreach (var item in order)
        {
            try { item.Reset(); }
            catch (Exception ex) { errors.Add($"{item.Name}: {ex.InnerException?.Message ?? ex.Message}"); }
        }
        if (errors.Count == 0)
            return Json(new { success = true, message = "All 12 settings reset from seed files successfully. You may need to log in again." });
        return Json(new { success = false, message = "Reset completed with errors: " + string.Join("; ", errors) });
    }
    
    private static string BuildPaginationQuery(string? search, string? sortColumn, string? sortDirection)
    {
        var qb = new List<string>();
        if (!string.IsNullOrEmpty(search)) qb.Add("search=" + Uri.EscapeDataString(search));
        if (!string.IsNullOrEmpty(sortColumn)) qb.Add("sortColumn=" + Uri.EscapeDataString(sortColumn));
        if (!string.IsNullOrEmpty(sortDirection)) qb.Add("sortDirection=" + Uri.EscapeDataString(sortDirection));
        return string.Join("&", qb);
    }
}
