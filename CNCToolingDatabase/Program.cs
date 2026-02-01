using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Repositories;
using CNCToolingDatabase.Services;
using CNCToolingDatabase.Middleware;

static bool ColumnExists(DbConnection conn, string table, string column)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"PRAGMA table_info({table})";
    using var r = cmd.ExecuteReader();
    while (r.Read())
        if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            return true;
    return false;
}

static void EnsureColumn(DbConnection conn, string table, string column, string typeAndDefault)
{
    if (ColumnExists(conn, table, column)) return;
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {typeAndDefault};";
    cmd.ExecuteNonQuery();
}

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
    // Add new columns only if missing (avoids failed ALTER and log noise when columns exist)
    var conn = context.Database.GetDbConnection();
    if (conn.State != ConnectionState.Open) conn.Open();
    try
    {
        EnsureColumn(conn, "ToolListDetails", "ToolPathTimeMinutes", "REAL NOT NULL DEFAULT 0");
        EnsureColumn(conn, "ToolListDetails", "Remarks", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "ToolListHeaders", "MachineModel", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "ToolListHeaders", "ApprovedBy", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "ToolListHeaders", "CamProgrammer", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "MachineNames", "Workcenter", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "MachineNames", "MachineModelId", "INTEGER NULL");
        EnsureColumn(conn, "MaterialSpecs", "MaterialSpecPurchased", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "MaterialSpecs", "MaterialSupplyConditionPurchased", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "MaterialSpecs", "MaterialType", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(conn, "Users", "IsActive", "INTEGER NOT NULL DEFAULT 1");
    }
    finally
    {
        if (conn.State == ConnectionState.Open) conn.Close();
    }
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
