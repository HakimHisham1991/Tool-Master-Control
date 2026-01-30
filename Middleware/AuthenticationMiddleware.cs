namespace CNCToolingDatabase.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] PublicPaths = { "/login", "/account/login", "/css", "/js", "/lib" };
    
    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }
        
        var userId = context.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            context.Response.Redirect("/login");
            return;
        }
        
        await _next(context);
    }
    
    private bool IsPublicPath(string path)
    {
        return PublicPaths.Any(p => path.StartsWith(p)) || path == "/";
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
