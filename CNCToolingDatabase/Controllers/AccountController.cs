using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Models.ViewModels;
using CNCToolingDatabase.Services;

namespace CNCToolingDatabase.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    
    public AccountController(IAuthService authService)
    {
        _authService = authService;
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
