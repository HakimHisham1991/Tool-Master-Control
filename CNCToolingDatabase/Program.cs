using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Repositories;
using CNCToolingDatabase.Services;
using CNCToolingDatabase.Middleware;
using CNCToolingDatabase.Helpers;
using Microsoft.Extensions.FileProviders;

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

static void EnsureTable(DbConnection conn, string createSql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = createSql;
    cmd.ExecuteNonQuery();
}

static void EnsureColumn(DbConnection conn, string table, string column, string typeAndDefault)
{
    if (ColumnExists(conn, table, column)) return;
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {typeAndDefault};";
    cmd.ExecuteNonQuery();
}


PdfFontBootstrap.EnsureInitialized();

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
builder.Services.AddScoped<PdfLayoutService>();

// Standalone/local binding only — shared hosts (e.g. MonsterASP) preconfigure ASPNETCORE_URLS.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

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
        EnsureColumn(conn, "ToolCodeUniques", "ItemCategory", "TEXT NOT NULL DEFAULT ''");
        EnsureTable(conn, """
            CREATE TABLE IF NOT EXISTS PdfLayouts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                LayoutJson TEXT NOT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedDate TEXT NOT NULL,
                UpdatedDate TEXT NOT NULL,
                CreatedBy TEXT NOT NULL DEFAULT ''
            );
            """);
    }
    finally
    {
        if (conn.State == ConnectionState.Open) conn.Close();
    }
    DbSeeder.Seed(context);

    var pdfLayoutService = scope.ServiceProvider.GetRequiredService<PdfLayoutService>();
    await pdfLayoutService.EnsureDefaultLayoutAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

var logoDir = Path.Combine(AppContext.BaseDirectory, "Data", "LOGO");
if (Directory.Exists(logoDir))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(logoDir),
        RequestPath = "/Data/LOGO"
    });
}

app.UseRouting();
app.UseSession();
app.UseCustomAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
