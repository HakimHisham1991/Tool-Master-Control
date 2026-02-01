using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Models.ViewModels;
using CNCToolingDatabase.Services;

namespace CNCToolingDatabase.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _dbContext;

    public AccountController(IAuthService authService, ApplicationDbContext dbContext)
    {
        _authService = authService;
        _dbContext = dbContext;
    }

    /// <summary>Development only: returns user count and usernames (no passwords) to verify login data.</summary>
    [HttpGet("/login/debug")]
    public async Task<IActionResult> LoginDebug()
    {
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return NotFound();
        var users = await _dbContext.Users
            .Select(u => new { u.Username, u.IsActive, PasswordLength = (u.Password ?? "").Length })
            .ToListAsync();
        return Json(new { userCount = users.Count, users });
    }

    [HttpGet("/login")]
    public IActionResult Login()
    {
        if (_authService.IsAuthenticated(HttpContext))
        {
            return RedirectToAction("Index", "ToolCodeUnique");
        }
        return View(new LoginViewModel());
    }
    
    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        
        var username = (model.Username ?? "").Trim();
        var password = (model.Password ?? "").Trim();
        var user = await _authService.AuthenticateAsync(username, password);
        if (user == null)
        {
            model.ErrorMessage = "Invalid username or password";
            return View(model);
        }
        
        _authService.SetUserSession(HttpContext, user);
        return RedirectToAction("Index", "ToolCodeUnique");
    }
    
    [HttpGet("/logout")]
    public IActionResult Logout()
    {
        _authService.ClearUserSession(HttpContext);
        return RedirectToAction("Login");
    }
}
