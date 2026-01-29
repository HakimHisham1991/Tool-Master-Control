using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Repositories;
using CNCToolingDatabase.Services;
using CNCToolingDatabase.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=CNCTooling.db"));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IToolListRepository, ToolListRepository>();
builder.Services.AddScoped<IToolMasterRepository, ToolMasterRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IToolCodeService, ToolCodeService>();
builder.Services.AddScoped<IToolCodeUniqueService, ToolCodeUniqueService>();
builder.Services.AddScoped<IToolListService, ToolListService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    // Add new columns if DB existed before they were added (EnsureCreated does not alter tables)
    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE ToolListDetails ADD COLUMN ToolPathTimeMinutes REAL NOT NULL DEFAULT 0;");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) { }
    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE ToolListDetails ADD COLUMN Remarks TEXT NOT NULL DEFAULT '';");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) { }
    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE ToolListHeaders ADD COLUMN MachineModel TEXT NOT NULL DEFAULT '';");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) { }
    DbSeeder.Seed(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseCustomAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
