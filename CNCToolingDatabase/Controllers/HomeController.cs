using Microsoft.AspNetCore.Mvc;

namespace CNCToolingDatabase.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "ToolCodeUnique");
        }
        return RedirectToAction("Login", "Account");
    }
}
