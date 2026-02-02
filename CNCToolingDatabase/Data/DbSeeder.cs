using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using CNCToolingDatabase.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace CNCToolingDatabase.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        // Ensure new tables exist (for databases created before these models were added)
        try
        {
            var connection = context.Database.GetDbConnection();
            connection.Open();
            try
            {
                // Add Website column to ToolSuppliers if it doesn't exist
                using (var checkCommand = connection.CreateCommand())
                {
                    checkCommand.CommandText = "PRAGMA table_info(ToolSuppliers);";
                    var hasWebsite = false;
                    using (var reader = checkCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetString(1) == "Website")
                            {
                                hasWebsite = true;
                                break;
                            }
                        }
                    }
                    if (!hasWebsite)
                    {
                        using var alterCommand = connection.CreateCommand();
                        alterCommand.CommandText = "ALTER TABLE ToolSuppliers ADD COLUMN Website TEXT;";
                        alterCommand.ExecuteNonQuery();
                    }
                }
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProjectCodes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Code TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    
                    CREATE TABLE IF NOT EXISTS MachineNames (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    
                    CREATE TABLE IF NOT EXISTS MachineWorkcenters (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Workcenter TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    
                    CREATE TABLE IF NOT EXISTS MachineModels (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Model TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    
                    CREATE TABLE IF NOT EXISTS CamLeaders (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS CamProgrammers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS Operations (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS Revisions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS PartNumbers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Description TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS MaterialSpecs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Spec TEXT NOT NULL,
                        Material TEXT NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE TABLE IF NOT EXISTS ToolCodeUniques (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SystemToolName TEXT NOT NULL,
                        ConsumableCode TEXT NOT NULL,
                        Supplier TEXT NOT NULL,
                        Diameter REAL NOT NULL,
                        FluteLength REAL NOT NULL,
                        CornerRadius REAL NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        LastModifiedDate TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS ToolSuppliers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Website TEXT,
                        Status TEXT NOT NULL,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL
                    );
                    
                    CREATE TABLE IF NOT EXISTS RunOnce (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Key TEXT NOT NULL UNIQUE,
                        DoneAt TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_ProjectCodes_Code ON ProjectCodes(Code);
                    CREATE INDEX IF NOT EXISTS IX_MachineNames_Name ON MachineNames(Name);
                    CREATE INDEX IF NOT EXISTS IX_MachineWorkcenters_Workcenter ON MachineWorkcenters(Workcenter);
                    CREATE INDEX IF NOT EXISTS IX_MachineModels_Model ON MachineModels(Model);
                    CREATE INDEX IF NOT EXISTS IX_CamLeaders_Name ON CamLeaders(Name);
                    CREATE INDEX IF NOT EXISTS IX_CamProgrammers_Name ON CamProgrammers(Name);
                    CREATE INDEX IF NOT EXISTS IX_Operations_Name ON Operations(Name);
                    CREATE INDEX IF NOT EXISTS IX_Revisions_Name ON Revisions(Name);
                    CREATE INDEX IF NOT EXISTS IX_PartNumbers_Name ON PartNumbers(Name);
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_MaterialSpecs_Spec_Material ON MaterialSpecs(Spec, Material);
                    CREATE INDEX IF NOT EXISTS IX_ToolSuppliers_Name ON ToolSuppliers(Name);
                ";
                command.ExecuteNonQuery();
                try { command.CommandText = "ALTER TABLE ProjectCodes ADD COLUMN Project TEXT;"; command.ExecuteNonQuery(); } catch { /* column may exist */ }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN ProjectCodeId INTEGER REFERENCES ProjectCodes(Id);"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN PartRev TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN DrawingRev TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN MaterialSpecId INTEGER REFERENCES MaterialSpecs(Id);"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN RefDrawing TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN Sequence INTEGER;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE MachineModels ADD COLUMN Type TEXT;"; command.ExecuteNonQuery(); } catch { }
                // One-time migration: allow duplicate Part Number names and add Sequence for Excel order
                command.CommandText = "SELECT 1 FROM RunOnce WHERE Key = 'PartNumbers_AllowDuplicateNames' LIMIT 1;";
                var alreadyMigrated = command.ExecuteScalar() != null;
                if (!alreadyMigrated)
                {
                    command.CommandText = @"CREATE TABLE PartNumbers_new (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        ProjectCodeId INTEGER,
                        PartRev TEXT,
                        DrawingRev TEXT,
                        MaterialSpecId INTEGER,
                        RefDrawing TEXT,
                        CreatedDate TEXT NOT NULL,
                        CreatedBy TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        Sequence INTEGER NOT NULL DEFAULT 0
                    );";
                    command.ExecuteNonQuery();
                    command.CommandText = @"INSERT INTO PartNumbers_new (Id, Name, Description, ProjectCodeId, PartRev, DrawingRev, MaterialSpecId, RefDrawing, CreatedDate, CreatedBy, IsActive, Sequence)
                        SELECT Id, Name, Description, ProjectCodeId, PartRev, DrawingRev, MaterialSpecId, RefDrawing, CreatedDate, CreatedBy, IsActive, COALESCE(Sequence, Id) FROM PartNumbers;";
                    command.ExecuteNonQuery();
                    command.CommandText = "DROP TABLE PartNumbers;";
                    command.ExecuteNonQuery();
                    command.CommandText = "ALTER TABLE PartNumbers_new RENAME TO PartNumbers;";
                    command.ExecuteNonQuery();
                    command.CommandText = "CREATE INDEX IF NOT EXISTS IX_PartNumbers_Name ON PartNumbers(Name);";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO RunOnce (Key, DoneAt) VALUES ('PartNumbers_AllowDuplicateNames', datetime('now'));";
                    command.ExecuteNonQuery();
                }
                try { command.CommandText = "ALTER TABLE MachineModels ADD COLUMN Controller TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE ToolListHeaders ADD COLUMN MaterialSpecId INTEGER REFERENCES MaterialSpecs(Id);"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE MaterialSpecs ADD COLUMN MaterialSpecPurchased TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE MaterialSpecs ADD COLUMN MaterialSupplyConditionPurchased TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE MaterialSpecs ADD COLUMN MaterialType TEXT;"; command.ExecuteNonQuery(); } catch { }
            }
            finally
            {
                connection.Close();
            }
        }
        catch
        {
            // Tables might already exist or be created by EF, ignore
        }
        
        // Seed users from USER MASTER.xlsx only when empty; no hard-coded fallback
        if (!context.Users.Any())
        {
            var userMasterPath = ResolveUserMasterPath();
            if (string.IsNullOrEmpty(userMasterPath))
                throw new InvalidOperationException("Missing file: USER MASTER.xlsx not found. Place the file in the Data folder.");
            var userRows = LoadUsersFromExcel(userMasterPath);
            if (userRows.Count == 0)
                throw new InvalidOperationException("No data loaded: USER MASTER.xlsx is empty or column headers do not match. Expected: Username, Password, Display Name; optional: Status.");
            foreach (var (username, password, displayName, isActive) in userRows)
            {
                context.Users.Add(new User { Username = username, Password = password, DisplayName = displayName, IsActive = isActive, CreatedDate = DateTime.UtcNow });
            }
            context.SaveChanges();
        }
        
        try
        {
            if (context.ProjectCodes != null && !context.ProjectCodes.Any())
            {
                foreach (var (code, description, project, isActive) in GetProjectCodeSeedData())
                {
                    context.ProjectCodes.Add(new ProjectCode { Code = code, Description = description, Project = project, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MachineModels != null && !context.MachineModels.Any())
            {
                foreach (var (model, description, type, controller, isActive) in GetMachineModelSeedData())
                {
                    context.MachineModels.Add(new MachineModel
                    {
                        Model = model,
                        Description = description,
                        Type = type,
                        Controller = controller,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "system",
                        IsActive = isActive
                    });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MachineNames != null)
            {
                context.MachineNames.RemoveRange(context.MachineNames.ToList());
                context.SaveChanges();
                var modelToId = (context.MachineModels ?? Enumerable.Empty<MachineModel>())
                    .ToDictionary(m => m.Model, m => m.Id, StringComparer.OrdinalIgnoreCase);
                foreach (var (name, serial, workcenter, machineModel, isActive) in GetMachineNameSeedData())
                {
                    var machineModelId = !string.IsNullOrWhiteSpace(machineModel) && modelToId.TryGetValue(machineModel.Trim(), out var id) ? id : (int?)null;
                    context.MachineNames.Add(new MachineName
                    {
                        Name = name,
                        Description = serial,
                        Workcenter = workcenter ?? "",
                        MachineModelId = machineModelId,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "system",
                        IsActive = isActive
                    });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MachineWorkcenters != null && !context.MachineWorkcenters.Any())
            {
                foreach (var (workcenter, axis, isActive) in GetMachineWorkcenterSeedData())
                {
                    context.MachineWorkcenters.Add(new MachineWorkcenter { Workcenter = workcenter, Axis = axis, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.CamLeaders != null && !context.CamLeaders.Any())
            {
                var camLeaderPath = ResolveCamLeaderPath();
                if (string.IsNullOrEmpty(camLeaderPath))
                    throw new InvalidOperationException("Missing file: CAM LEADER - MASTER.xlsx not found. Place the file in the Data folder.");
                var camLeaderRows = LoadCamLeaderFromExcel(camLeaderPath);
                if (camLeaderRows.Count == 0)
                    throw new InvalidOperationException("No data loaded: CAM LEADER - MASTER.xlsx is empty or column headers do not match. Expected: Name, Position; optional: Status (ACTIVE/INACTIVE).");
                foreach (var (name, position, isActive) in camLeaderRows)
                {
                    context.CamLeaders.Add(new CamLeader { Name = name, Description = position ?? "", CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
                }
                context.SaveChanges();
            }
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception) { }
        
        try
        {
            if (context.CamProgrammers != null && !context.CamProgrammers.Any())
            {
                var camProgrammerPath = ResolveCamProgrammerPath();
                if (string.IsNullOrEmpty(camProgrammerPath))
                    throw new InvalidOperationException("Missing file: CAM PROGRAMMER - MASTER.xlsx not found. Place the file in the Data folder.");
                var camProgrammerRows = LoadCamProgrammerFromExcel(camProgrammerPath);
                if (camProgrammerRows.Count == 0)
                    throw new InvalidOperationException("No data loaded: CAM PROGRAMMER - MASTER.xlsx is empty or column headers do not match. Expected: Name, Location; optional: Status (ACTIVE/INACTIVE).");
                foreach (var (name, location, isActive) in camProgrammerRows)
                {
                    context.CamProgrammers.Add(new CamProgrammer { Name = name, Description = location ?? "", CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
                }
                context.SaveChanges();
            }
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception) { }
        
        try
        {
            if (context.Operations != null && !context.Operations.Any())
            {
                var operationSeed = new[] { "OP10", "OP20", "OP30", "OP40", "OP50", "OP60", "OP70", "OP80", "OP90" };
                foreach (var name in operationSeed)
                {
                    context.Operations.Add(new Operation { Name = name, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.Revisions != null && !context.Revisions.Any())
            {
                var revisionSeed = new[] { "REV00", "REV01", "REV02", "REV03", "REV04", "REV05", "REV06", "REV07", "REV08", "REV09", "REV10" };
                foreach (var name in revisionSeed)
                {
                    context.Revisions.Add(new Revision { Name = name, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MaterialSpecs != null && !context.MaterialSpecs.Any())
            {
                var excelPath = ResolveMaterialSpecMasterPath();
                if (string.IsNullOrEmpty(excelPath))
                    throw new InvalidOperationException("Missing file: MATERIAL SPEC MASTER.xlsx not found. Place the file in the Data folder.");
                var rows = LoadMaterialSpecFromExcel(excelPath);
                if (rows.Count == 0)
                    throw new InvalidOperationException("No data loaded: MATERIAL SPEC MASTER.xlsx is empty or column headers do not match. Expected headers: Material Specification (On Drawing), General Name; optional: Material Specification (Purchased), Material Supply Condition (Purchased), Material Type, Status.");
                var seen = new HashSet<(string, string)>();
                foreach (var (spec, specPurchased, material, supplyCondition, materialType, isActive) in rows)
                {
                    var key = ((spec ?? "").ToLowerInvariant(), (material ?? "").ToLowerInvariant());
                    if (seen.Contains(key)) continue;
                    seen.Add(key);
                    context.MaterialSpecs.Add(new MaterialSpec
                    {
                        Spec = spec ?? "",
                        MaterialSpecPurchased = specPurchased ?? "",
                        Material = material ?? "",
                        MaterialSupplyConditionPurchased = supplyCondition ?? "",
                        MaterialType = materialType ?? "",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "system",
                        IsActive = isActive
                    });
                }
                context.SaveChanges();
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
        
        try
        {
            if (context.ToolSuppliers != null && !context.ToolSuppliers.Any())
            {
                var toolSupplierPath = ResolveToolSupplierMasterPath();
                if (string.IsNullOrEmpty(toolSupplierPath))
                    throw new InvalidOperationException("Missing file: TOOL SUPPLIER MASTER.xlsx not found. Place the file in the Data folder.");
                var rows = LoadToolSupplierFromExcel(toolSupplierPath);
                if (rows.Count == 0)
                    throw new InvalidOperationException("No data loaded: TOOL SUPPLIER MASTER.xlsx is empty or column headers do not match. Expected: Supplier or Tool Supplier or Name; Website; Status.");
                foreach (var (name, website, status) in rows)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    context.ToolSuppliers.Add(new ToolSupplier
                    {
                        Name = name ?? "",
                        Website = website,
                        Status = status ?? "",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "system"
                    });
                }
                context.SaveChanges();
            }
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception) { }
        
        try
        {
            if (context.PartNumbers != null && !context.PartNumbers.Any())
            {
                var projectCodes = (context.ProjectCodes ?? Enumerable.Empty<ProjectCode>()).ToDictionary(p => p.Code, p => p.Id);
                var materialSpecs = (context.MaterialSpecs ?? Enumerable.Empty<MaterialSpec>()).ToList();
                var matBySpec = materialSpecs.GroupBy(m => m.Spec).ToDictionary(g => g.Key, g => g.First().Id);
                foreach (var (name, desc, partRev, drawRev, pcCode, refDrawing, msSpec, sequence) in GetPartNumberSeedData())
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var pcId = !string.IsNullOrEmpty(pcCode) && projectCodes.TryGetValue(pcCode, out var pid) ? pid : (int?)null;
                    var msId = !string.IsNullOrEmpty(msSpec) && matBySpec.TryGetValue(msSpec, out var mid) ? mid : (int?)null;
                    context.PartNumbers.Add(new PartNumber
                    {
                        Name = name,
                        Description = desc,
                        ProjectCodeId = pcId,
                        PartRev = partRev,
                        DrawingRev = drawRev,
                        MaterialSpecId = msId,
                        RefDrawing = refDrawing ?? "",
                        Sequence = sequence,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "system",
                        IsActive = true
                    });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        if (!context.ToolListHeaders.Any())
        {
            var excelPath = Path.Combine(AppContext.BaseDirectory, "Data", "TOOL CODE MASTER.xlsx");
            var toolCodeList = LoadToolCodeUniqueFromExcel(excelPath);
            // Group by ConsumableCode (Item2) and take first row per key so duplicate codes in Excel do not throw
            var masterLookup = toolCodeList
                .GroupBy(t => t.ConsumableCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => { var f = g.First(); return (f.SystemToolName, f.Supplier, f.Diameter, f.FluteLength, f.CornerRadius); }, StringComparer.OrdinalIgnoreCase);
            var toolLists = new List<ToolListHeader>
            {
                CreateToolListWithDetails("V5754221420001", "OP10", "REV00", "AG01", "S001", "2X-01", "DMU50", "hakim.hisham", masterLookup),
                CreateToolListWithDetails("V5754221420001", "OP20", "REV00", "AG01", "S001", "2X-01", "DMU50", "hakim.hisham", masterLookup),
                CreateToolListWithDetails("351-2180-7", "OP10", "REV00", "AG02", "SP11", "5X-01", "VCN510C", "adib.jamil", masterLookup),
                CreateToolListWithDetails("E5757332620000", "OP10", "REV00", "AL01", "K5-42", "3X-07", "Integrex i-200", "adib.jamil", masterLookup),
                CreateToolListWithDetails("E5757332620000", "OP20", "REV00", "AL01", "K5-42", "3X-07", "Integrex i-200", "nik.faiszal", masterLookup),
            };
            
            context.ToolListHeaders.AddRange(toolLists);
            context.SaveChanges();
            
            var processedCodes = new HashSet<string>();
            foreach (var header in toolLists)
            {
                foreach (var detail in header.Details)
                {
                    if (!string.IsNullOrWhiteSpace(detail.ConsumableCode) && 
                        !processedCodes.Contains(detail.ConsumableCode))
                    {
                        UpdateToolMaster(context, detail);
                        processedCodes.Add(detail.ConsumableCode);
                        context.SaveChanges();
                    }
                }
            }
        }
        
        if (context.ToolCodeUniques != null && !context.ToolCodeUniques.Any())
        {
            var excelPath = Path.Combine(AppContext.BaseDirectory, "Data", "TOOL CODE MASTER.xlsx");
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var (systemName, consumable, supplier, dia, flute, radius) in LoadToolCodeUniqueFromExcel(excelPath))
            {
                context.ToolCodeUniques.Add(new ToolCodeUnique
                {
                    SystemToolName = systemName,
                    ConsumableCode = consumable,
                    Supplier = supplier,
                    Diameter = dia,
                    FluteLength = flute,
                    CornerRadius = radius,
                    CreatedDate = baseTime,
                    LastModifiedDate = baseTime
                });
            }
            context.SaveChanges();
        }
    }

    /// <summary>Load Machine Name rows from MACHINE NAME MASTER.xlsx. Columns: Machine Name, Serial Number, Machine Workcenter, Machine Model, Status.</summary>
    private static List<(string Name, string Serial, string Workcenter, string MachineModel, bool IsActive)> LoadMachineNameFromExcel(string path)
    {
        var result = new List<(string, string, string, string, bool)>();
        if (!File.Exists(path)) return result;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo(path));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null) return result;
        int cols = ws.Dimension.End.Column;
        int rows = ws.Dimension.End.Row;
        if (rows < 2) return result;
        static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(v)) continue;
                foreach (var h in headerNames)
                    if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return -1;
        }
        int colName = GetCol(ws, cols, "Machine Name", "Name");
        int colSerial = GetCol(ws, cols, "Serial Number", "Serial");
        int colWorkcenter = GetCol(ws, cols, "Machine Workcenter", "Workcenter");
        int colModel = GetCol(ws, cols, "Machine Model", "Model");
        int colStatus = GetCol(ws, cols, "Status", "IsActive");
        if (colName < 1) return result;
        static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
        for (int r = 2; r <= rows; r++)
        {
            var name = GetStr(ws, r, colName);
            if (string.IsNullOrWhiteSpace(name)) continue;
            var serial = GetStr(ws, r, colSerial);
            var workcenter = GetStr(ws, r, colWorkcenter);
            var machineModel = GetStr(ws, r, colModel);
            var statusVal = GetStr(ws, r, colStatus);
            var isActive = string.IsNullOrWhiteSpace(statusVal) ||
                string.Equals(statusVal, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusVal, "1", StringComparison.Ordinal) ||
                string.Equals(statusVal, "Yes", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(statusVal, "INACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(statusVal, "0", StringComparison.Ordinal) || string.Equals(statusVal, "No", StringComparison.OrdinalIgnoreCase))
                isActive = false;
            result.Add((name, serial ?? "", workcenter ?? "", machineModel ?? "", isActive));
        }
        return result;
    }

    private static (string Name, string Serial, string Workcenter, string MachineModel, bool IsActive)[] GetMachineNameSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "MACHINE NAME MASTER.xlsx");
        return LoadMachineNameFromExcel(path).ToArray();
    }

    /// <summary>Load Machine Model rows from MACHINE MODEL MASTER.xlsx. Columns: Model, Description (or Machine Builder), Type, Controller, Status (ACTIVE/INACTIVE).</summary>
    private static List<(string Model, string Description, string Type, string Controller, bool IsActive)> LoadMachineModelFromExcel(string path)
    {
        var result = new List<(string, string, string, string, bool)>();
        if (!File.Exists(path)) return result;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo(path));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null) return result;
        int cols = ws.Dimension.End.Column;
        int rows = ws.Dimension.End.Row;
        if (rows < 2) return result;
        static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(v)) continue;
                foreach (var h in headerNames)
                    if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return -1;
        }
        int colModel = GetCol(ws, cols, "Model", "Machine Model");
        int colDescription = GetCol(ws, cols, "Description", "Machine Builder");
        int colType = GetCol(ws, cols, "Type");
        int colController = GetCol(ws, cols, "Controller");
        int colStatus = GetCol(ws, cols, "Status", "IsActive");
        if (colModel < 1) return result;
        static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
        for (int r = 2; r <= rows; r++)
        {
            var model = GetStr(ws, r, colModel);
            if (string.IsNullOrWhiteSpace(model)) continue;
            var description = GetStr(ws, r, colDescription);
            var type = GetStr(ws, r, colType);
            var controller = GetStr(ws, r, colController);
            var statusVal = GetStr(ws, r, colStatus);
            var isActive = string.IsNullOrWhiteSpace(statusVal) ||
                string.Equals(statusVal, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusVal, "1", StringComparison.Ordinal) ||
                string.Equals(statusVal, "Yes", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(statusVal, "INACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(statusVal, "0", StringComparison.Ordinal) || string.Equals(statusVal, "No", StringComparison.OrdinalIgnoreCase))
                isActive = false;
            result.Add((model, description ?? "", type ?? "", controller ?? "", isActive));
        }
        return result;
    }

    private static (string Model, string Description, string Type, string Controller, bool IsActive)[] GetMachineModelSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "MACHINE MODEL MASTER.xlsx");
        return LoadMachineModelFromExcel(path).ToArray();
    }

    /// <summary>Load Project Code rows from PROJECT CODE MASTER.xlsx. Columns: Code, Description (or Customer), Project, Status (ACTIVE/INACTIVE).</summary>
    private static List<(string Code, string Description, string Project, bool IsActive)> LoadProjectCodeFromExcel(string path)
    {
        var result = new List<(string, string, string, bool)>();
        if (!File.Exists(path)) return result;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo(path));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null) return result;
        int cols = ws.Dimension.End.Column;
        int rows = ws.Dimension.End.Row;
        if (rows < 2) return result;
        static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(v)) continue;
                foreach (var h in headerNames)
                    if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return -1;
        }
        int colCode = GetCol(ws, cols, "Code", "Project Code");
        int colDescription = GetCol(ws, cols, "Description", "Customer");
        int colProject = GetCol(ws, cols, "Project");
        int colStatus = GetCol(ws, cols, "Status", "IsActive");
        if (colCode < 1) return result;
        static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
        for (int r = 2; r <= rows; r++)
        {
            var code = GetStr(ws, r, colCode);
            if (string.IsNullOrWhiteSpace(code)) continue;
            var description = GetStr(ws, r, colDescription);
            var project = GetStr(ws, r, colProject);
            var statusVal = GetStr(ws, r, colStatus);
            var isActive = string.IsNullOrWhiteSpace(statusVal) ||
                string.Equals(statusVal, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusVal, "1", StringComparison.Ordinal) ||
                string.Equals(statusVal, "Yes", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(statusVal, "INACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(statusVal, "0", StringComparison.Ordinal) || string.Equals(statusVal, "No", StringComparison.OrdinalIgnoreCase))
                isActive = false;
            result.Add((code, description ?? "", project ?? "", isActive));
        }
        return result;
    }

    private static (string Code, string Description, string Project, bool IsActive)[] GetProjectCodeSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "PROJECT CODE MASTER.xlsx");
        return LoadProjectCodeFromExcel(path).ToArray();
    }

    /// <summary>Load Machine Workcenter rows from MACHINE WORKCENTER MASTER.xlsx. Columns: Workcenter, Axis, Status (ACTIVE/INACTIVE).</summary>
    private static List<(string Workcenter, string Axis, bool IsActive)> LoadMachineWorkcenterFromExcel(string path)
    {
        var result = new List<(string, string, bool)>();
        if (!File.Exists(path)) return result;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo(path));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null) return result;
        int cols = ws.Dimension.End.Column;
        int rows = ws.Dimension.End.Row;
        if (rows < 2) return result;
        static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(v)) continue;
                foreach (var h in headerNames)
                    if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return -1;
        }
        int colWorkcenter = GetCol(ws, cols, "Workcenter", "Machine Workcenter");
        int colAxis = GetCol(ws, cols, "Axis");
        int colStatus = GetCol(ws, cols, "Status", "IsActive");
        if (colWorkcenter < 1) return result;
        static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
        for (int r = 2; r <= rows; r++)
        {
            var workcenter = GetStr(ws, r, colWorkcenter);
            if (string.IsNullOrWhiteSpace(workcenter)) continue;
            var axis = GetStr(ws, r, colAxis);
            var statusVal = GetStr(ws, r, colStatus);
            var isActive = string.IsNullOrWhiteSpace(statusVal) ||
                string.Equals(statusVal, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusVal, "1", StringComparison.Ordinal) ||
                string.Equals(statusVal, "Yes", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(statusVal, "INACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(statusVal, "0", StringComparison.Ordinal) || string.Equals(statusVal, "No", StringComparison.OrdinalIgnoreCase))
                isActive = false;
            result.Add((workcenter, axis ?? "", isActive));
        }
        return result;
    }

    private static (string Workcenter, string Axis, bool IsActive)[] GetMachineWorkcenterSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "MACHINE WORKCENTER MASTER.xlsx");
        return LoadMachineWorkcenterFromExcel(path).ToArray();
    }

    /// <summary>Hard-coded tool list seed. Each (partNumber, operation) has fixed consumable codes from Master. Same every reset.</summary>
    private static (string PartNumber, string Operation, string[] ConsumableCodes)[] GetToolListDetailsSeedData()
    {
        return new[]
        {
            ("V5754221420001", "OP10", new[] { "553120Z3.0-SIRON-A", "553160R050Z3.0-SIRON-A", "553080Z3.0-SIRON-A", "553100Z3.0-SIRON-A", "553040Z3.0-SIRON-A" }),
            ("V5754221420001", "OP20", new[] { "553100Z3.0-SIRON-A", "A3389DPL-9.8", "A3389DPL-12", "7792VXP06CA016Z2R140", "7792VXD09WA032Z3R", "553060Z3.0-SIRON-A" }),
            ("351-2180-7", "OP10", new[] { "553140Z3.0-SIRON-A", "553180R100Z3.0-SIRON-A", "553200Z3.0-SIRON-A", "553080Z3.0-SIRON-A", "553100R250Z3.0-SIRON-A", "553120R025Z3.0-SIRON-A", "7792VXD12-A052Z5R" }),
            ("E5757332620000", "OP10", new[] { "7792VXD12-A080Z8R", "553040Z3.0-SIRON-A", "553060Z3.0-SIRON-A", "553080Z3.0-SIRON-A", "A3389DPL-6.1" }),
            ("E5757332620000", "OP20", new[] { "553100Z3.0-SIRON-A", "553120Z3.0-SIRON-A", "7792VXP06CA016Z2R140", "7792VXD09WA032Z3R", "553140Z3.0-SIRON-A", "553160R050Z3.0-SIRON-A" }),
        };
    }

    /// <summary>Load Part Number rows from PART NUMBERS MASTER.xlsx. Columns: Part Number, Description, Part Revision, Drawing Revision, Project Code, Ref. Drawing, Material Spec., Material. Sequence is 1-based Excel row order.</summary>
    private static List<(string Name, string Description, string PartRev, string DrawingRev, string ProjectCode, string RefDrawing, string MaterialSpec, int Sequence)> LoadPartNumberFromExcel(string path)
    {
        var result = new List<(string, string, string, string, string, string, string, int)>();
        if (!File.Exists(path)) return result;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo(path));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null) return result;
        int cols = ws.Dimension.End.Column;
        int rows = ws.Dimension.End.Row;
        if (rows < 2) return result;
        static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(v)) continue;
                foreach (var h in headerNames)
                    if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return -1;
        }
        int colPartNumber = GetCol(ws, cols, "Part Number", "Name");
        int colDescription = GetCol(ws, cols, "Description");
        int colPartRev = GetCol(ws, cols, "Part Revision", "Part Rev.");
        int colDrawingRev = GetCol(ws, cols, "Drawing Revision", "Drawing Rev.");
        int colProjectCode = GetCol(ws, cols, "Project Code");
        int colRefDrawing = GetCol(ws, cols, "Ref. Drawing", "Ref Drawing");
        int colMaterialSpec = GetCol(ws, cols, "Material Spec.", "Material Spec");
        if (colPartNumber < 1) return result;
        static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
        int sequence = 0;
        for (int r = 2; r <= rows; r++)
        {
            var name = GetStr(ws, r, colPartNumber);
            if (string.IsNullOrWhiteSpace(name)) continue;
            sequence++;
            var desc = GetStr(ws, r, colDescription);
            var partRev = GetStr(ws, r, colPartRev);
            var drawRev = GetStr(ws, r, colDrawingRev);
            var pcCode = GetStr(ws, r, colProjectCode);
            var refDrawing = GetStr(ws, r, colRefDrawing);
            var msSpec = GetStr(ws, r, colMaterialSpec);
            result.Add((name, desc ?? "", partRev ?? "", drawRev ?? "", pcCode ?? "", refDrawing ?? "", msSpec ?? "", sequence));
        }
        return result;
    }

    /// <summary>Resolve path to USER MASTER.xlsx: try output Data folder, then current directory Data folder, then project Data folder (walk up from bin).</summary>
    private static string? ResolveUserMasterPath()
    {
        const string fileName = "USER MASTER.xlsx";
        var baseData = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        if (File.Exists(baseData)) return baseData;
        var currentData = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
        if (File.Exists(currentData)) return currentData;
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "Data", fileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
        return null;
    }

    /// <summary>Load User rows from USER MASTER.xlsx. Throws on file not found or cannot open/corrupted. Columns: Username, Password, Display Name; optional: Status (ACTIVE/INACTIVE).</summary>
    private static List<(string Username, string Password, string DisplayName, bool IsActive)> LoadUsersFromExcel(string path)
    {
        var result = new List<(string, string, string, bool)>();
        if (string.IsNullOrEmpty(path))
            return result;
        if (!File.Exists(path))
            throw new InvalidOperationException("Missing file: USER MASTER.xlsx not found at " + path + ". Place the file in the Data folder.");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        try
        {
            using var package = new ExcelPackage(new FileInfo(path));
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws?.Dimension == null) return result;
            int cols = ws.Dimension.End.Column;
            int rows = ws.Dimension.End.Row;
            if (rows < 2) return result;
            static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
            {
                for (int c = 1; c <= totalCols; c++)
                {
                    var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(v)) continue;
                    foreach (var h in headerNames)
                        if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
                }
                return -1;
            }
            int colUsername = GetCol(ws, cols, "Username", "User Name");
            int colPassword = GetCol(ws, cols, "Password");
            int colDisplayName = GetCol(ws, cols, "Display Name", "DisplayName");
            int colStatus = GetCol(ws, cols, "Status", "Active", "IsActive");
            if (colUsername < 1 || colPassword < 1) return result;
            static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
            static string GetPasswordStr(ExcelWorksheet sheet, int row, int col)
            {
                if (col < 1) return "";
                var v = sheet.Cells[row, col].Value;
                if (v == null) return "";
                if (v is double d) return d == Math.Floor(d) ? ((long)d).ToString() : d.ToString(CultureInfo.InvariantCulture);
                if (v is float f) return f == Math.Floor(f) ? ((long)f).ToString() : f.ToString(CultureInfo.InvariantCulture);
                if (v is long or int) return v.ToString() ?? "";
                return v.ToString()?.Trim() ?? "";
            }
            static bool ParseStatusActive(ExcelWorksheet sheet, int row, int col)
            {
                var val = GetStr(sheet, row, col);
                if (string.IsNullOrWhiteSpace(val)) return true;
                if (string.Equals(val, "INACTIVE", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "NO", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "0", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }
            for (int r = 2; r <= rows; r++)
            {
                var username = GetStr(ws, r, colUsername);
                if (string.IsNullOrWhiteSpace(username)) continue;
                var password = GetPasswordStr(ws, r, colPassword);
                var displayName = GetStr(ws, r, colDisplayName);
                var isActive = colStatus >= 1 ? ParseStatusActive(ws, r, colStatus) : true;
                result.Add((username, password ?? "", displayName ?? username, isActive));
            }
            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cannot open file or file corrupted: " + ex.Message, ex);
        }
    }

    /// <summary>Resolve path to CAM PROGRAMMER - MASTER.xlsx: try output Data folder, then current directory Data folder, then project Data folder (walk up from bin).</summary>
    private static string? ResolveCamProgrammerPath()
    {
        const string fileName = "CAM PROGRAMMER - MASTER.xlsx";
        var baseData = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        if (File.Exists(baseData)) return baseData;
        var currentData = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
        if (File.Exists(currentData)) return currentData;
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "Data", fileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
        return null;
    }

    /// <summary>Load CAM Programmer rows from CAM PROGRAMMER - MASTER.xlsx. Throws on file not found or cannot open/corrupted. Columns: No., Name, Location; optional: Status (ACTIVE/INACTIVE).</summary>
    private static List<(string Name, string? Location, bool IsActive)> LoadCamProgrammerFromExcel(string path)
    {
        var result = new List<(string, string?, bool)>();
        if (string.IsNullOrEmpty(path))
            return result;
        if (!File.Exists(path))
            throw new InvalidOperationException("Missing file: CAM PROGRAMMER - MASTER.xlsx not found at " + path + ". Place the file in the Data folder.");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        try
        {
            using var package = new ExcelPackage(new FileInfo(path));
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws?.Dimension == null) return result;
            int cols = ws.Dimension.End.Column;
            int rows = ws.Dimension.End.Row;
            if (rows < 2) return result;
            static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
            {
                for (int c = 1; c <= totalCols; c++)
                {
                    var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(v)) continue;
                    foreach (var h in headerNames)
                        if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
                }
                return -1;
            }
            int colName = GetCol(ws, cols, "Name");
            int colLocation = GetCol(ws, cols, "Location");
            int colStatus = GetCol(ws, cols, "Status", "Active", "IsActive");
            if (colName < 1) return result;
            static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
            static bool ParseStatusActive(ExcelWorksheet sheet, int row, int col)
            {
                var val = GetStr(sheet, row, col);
                if (string.IsNullOrWhiteSpace(val)) return true;
                if (string.Equals(val, "INACTIVE", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "NO", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "0", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }
            for (int r = 2; r <= rows; r++)
            {
                var name = GetStr(ws, r, colName);
                if (string.IsNullOrWhiteSpace(name)) continue;
                var location = colLocation >= 1 ? GetStr(ws, r, colLocation) : null;
                var isActive = colStatus >= 1 ? ParseStatusActive(ws, r, colStatus) : true;
                result.Add((name, string.IsNullOrWhiteSpace(location) ? null : location, isActive));
            }
            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cannot open file or file corrupted: " + ex.Message, ex);
        }
    }

    /// <summary>Resolve path to CAM LEADER - MASTER.xlsx: try output Data folder, then current directory Data folder, then project Data folder (walk up from bin).</summary>
    private static string? ResolveCamLeaderPath()
    {
        const string fileName = "CAM LEADER - MASTER.xlsx";
        var baseData = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        if (File.Exists(baseData)) return baseData;
        var currentData = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
        if (File.Exists(currentData)) return currentData;
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "Data", fileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
        return null;
    }

    /// <summary>Load CAM Leader rows from CAM LEADER - MASTER.xlsx. Throws on file not found or cannot open/corrupted. Columns: No., Name, Position; optional: Status (ACTIVE/INACTIVE).</summary>
    private static List<(string Name, string? Position, bool IsActive)> LoadCamLeaderFromExcel(string path)
    {
        var result = new List<(string, string?, bool)>();
        if (string.IsNullOrEmpty(path))
            return result;
        if (!File.Exists(path))
            throw new InvalidOperationException("Missing file: CAM LEADER - MASTER.xlsx not found at " + path + ". Place the file in the Data folder.");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        try
        {
            using var package = new ExcelPackage(new FileInfo(path));
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws?.Dimension == null) return result;
            int cols = ws.Dimension.End.Column;
            int rows = ws.Dimension.End.Row;
            if (rows < 2) return result;
            static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
            {
                for (int c = 1; c <= totalCols; c++)
                {
                    var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(v)) continue;
                    foreach (var h in headerNames)
                        if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
                }
                return -1;
            }
            int colName = GetCol(ws, cols, "Name");
            int colPosition = GetCol(ws, cols, "Position");
            int colStatus = GetCol(ws, cols, "Status", "Active", "IsActive");
            if (colName < 1) return result;
            static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
            static bool ParseStatusActive(ExcelWorksheet sheet, int row, int col)
            {
                var val = GetStr(sheet, row, col);
                if (string.IsNullOrWhiteSpace(val)) return true;
                if (string.Equals(val, "INACTIVE", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "NO", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "0", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }
            for (int r = 2; r <= rows; r++)
            {
                var name = GetStr(ws, r, colName);
                if (string.IsNullOrWhiteSpace(name)) continue;
                var position = colPosition >= 1 ? GetStr(ws, r, colPosition) : null;
                var isActive = colStatus >= 1 ? ParseStatusActive(ws, r, colStatus) : true;
                result.Add((name, string.IsNullOrWhiteSpace(position) ? null : position, isActive));
            }
            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cannot open file or file corrupted: " + ex.Message, ex);
        }
    }

    /// <summary>Resolve path to TOOL SUPPLIER MASTER.xlsx: try output Data folder, then current directory Data folder, then project Data folder (walk up from bin).</summary>
    private static string? ResolveToolSupplierMasterPath()
    {
        const string fileName = "TOOL SUPPLIER MASTER.xlsx";
        var baseData = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        if (File.Exists(baseData)) return baseData;
        var currentData = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
        if (File.Exists(currentData)) return currentData;
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "Data", fileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
        return null;
    }

    /// <summary>Load Tool Supplier rows from TOOL SUPPLIER MASTER.xlsx. Columns: Supplier or Tool Supplier or Name; Website; Status (exact value from Excel).</summary>
    private static List<(string Name, string? Website, string Status)> LoadToolSupplierFromExcel(string? path)
    {
        var result = new List<(string, string?, string)>();
        if (string.IsNullOrEmpty(path))
            return result;
        if (!File.Exists(path))
            throw new InvalidOperationException("Missing file: TOOL SUPPLIER MASTER.xlsx not found at " + path + ". Place the file in the Data folder.");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        try
        {
            using var package = new ExcelPackage(new FileInfo(path));
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws?.Dimension == null) return result;
            int cols = ws.Dimension.End.Column;
            int rows = ws.Dimension.End.Row;
            if (rows < 2) return result;
            static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
            {
                for (int c = 1; c <= totalCols; c++)
                {
                    var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(v)) continue;
                    foreach (var h in headerNames)
                        if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
                }
                return -1;
            }
            int colName = GetCol(ws, cols, "Supplier", "Tool Supplier", "Name");
            int colWebsite = GetCol(ws, cols, "Website", "URL", "Link");
            int colStatus = GetCol(ws, cols, "Status");
            if (colName < 1) return result;
            static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
            for (int r = 2; r <= rows; r++)
            {
                var name = GetStr(ws, r, colName);
                var website = colWebsite >= 1 ? GetStr(ws, r, colWebsite) : null;
                var status = colStatus >= 1 ? GetStr(ws, r, colStatus) : "";
                if (string.IsNullOrWhiteSpace(name)) continue;
                result.Add((name ?? "", string.IsNullOrWhiteSpace(website) ? null : website, status ?? ""));
            }
            return result;
        }
        catch (InvalidOperationException) { throw; }
        catch (Exception ex) { throw new InvalidOperationException("Cannot open file or file corrupted: " + ex.Message, ex); }
    }

    /// <summary>Resolve path to MATERIAL SPEC MASTER.xlsx: try output Data folder, then current directory Data folder, then project Data folder (walk up from bin).</summary>
    private static string? ResolveMaterialSpecMasterPath()
    {
        const string fileName = "MATERIAL SPEC MASTER.xlsx";
        var baseData = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
        if (File.Exists(baseData)) return baseData;
        var currentData = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
        if (File.Exists(currentData)) return currentData;
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
        {
            var candidate = Path.Combine(dir, "Data", fileName);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }
        return null;
    }

    /// <summary>Load Material Spec rows from MATERIAL SPEC MASTER.xlsx. Throws on missing path, file not found, or cannot open/corrupted. Returns empty list only when file has no data or headers don't match.</summary>
    private static List<(string Spec, string MaterialSpecPurchased, string Material, string MaterialSupplyConditionPurchased, string MaterialType, bool IsActive)> LoadMaterialSpecFromExcel(string? path)
    {
        var result = new List<(string, string, string, string, string, bool)>();
        if (string.IsNullOrEmpty(path))
            return result;
        if (!File.Exists(path))
            throw new InvalidOperationException("Missing file: MATERIAL SPEC MASTER.xlsx not found at " + path + ". Place the file in the Data folder.");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        try
        {
            using var package = new ExcelPackage(new FileInfo(path));
            var ws = package.Workbook.Worksheets.FirstOrDefault();
            if (ws?.Dimension == null) return result;
            int cols = ws.Dimension.End.Column;
            int rows = ws.Dimension.End.Row;
            if (rows < 2) return result;
            static int GetCol(ExcelWorksheet sheet, int totalCols, params string[] headerNames)
            {
                for (int c = 1; c <= totalCols; c++)
                {
                    var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(v)) continue;
                    foreach (var h in headerNames)
                        if (string.Equals(v, h, StringComparison.OrdinalIgnoreCase)) return c;
                }
                return -1;
            }
            int colSpec = GetCol(ws, cols, "Material Specification (On Drawing)", "Material Spec. (On Drawing)", "Material Spec.");
            int colSpecPurchased = GetCol(ws, cols, "Material Specification (Purchased)", "Material Spec. (Purchased)");
            int colMaterial = GetCol(ws, cols, "General Name", "Material");
            int colSupplyCondition = GetCol(ws, cols, "Material Supply Condition (Purchased)", "Material Supply Condition");
            int colMaterialType = GetCol(ws, cols, "Material Type");
            int colStatus = GetCol(ws, cols, "Status", "Active", "IsActive");
            if (colSpec < 1 && colMaterial < 1) return result;
            static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
            static bool ParseStatusActive(ExcelWorksheet sheet, int row, int col)
            {
                var val = GetStr(sheet, row, col);
                if (string.IsNullOrWhiteSpace(val)) return true;
                if (string.Equals(val, "INACTIVE", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "NO", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(val, "0", StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }
            for (int r = 2; r <= rows; r++)
            {
                var spec = GetStr(ws, r, colSpec);
                var specPurchased = GetStr(ws, r, colSpecPurchased);
                var material = GetStr(ws, r, colMaterial);
                var supplyCondition = GetStr(ws, r, colSupplyCondition);
                var materialType = GetStr(ws, r, colMaterialType);
                var isActive = colStatus >= 1 ? ParseStatusActive(ws, r, colStatus) : true;
                if (string.IsNullOrWhiteSpace(spec) && string.IsNullOrWhiteSpace(material)) continue;
                result.Add((spec ?? "", specPurchased ?? "", material ?? "", supplyCondition ?? "", materialType ?? "", isActive));
            }
            return result;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cannot open file or file corrupted: " + ex.Message, ex);
        }
    }

    private static (string Name, string Description, string PartRev, string DrawingRev, string ProjectCode, string RefDrawing, string MaterialSpec, int Sequence)[] GetPartNumberSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "PART NUMBERS MASTER.xlsx");
        return LoadPartNumberFromExcel(path).ToArray();
    }

    private static ToolListHeader CreateToolListWithDetails(string partNumber, string operation, string revision,
        string projectCode, string machineName, string workcenter, string machineModel, string createdBy,
        IReadOnlyDictionary<string, (string SystemToolName, string Supplier, decimal Dia, decimal Flute, decimal Radius)> masterLookup)
    {
        var baseDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var header = new ToolListHeader
        {
            PartNumber = partNumber,
            Operation = operation,
            Revision = revision,
            ProjectCode = projectCode,
            MachineName = machineName,
            MachineWorkcenter = workcenter,
            MachineModel = machineModel,
            CreatedBy = createdBy,
            CreatedDate = baseDate,
            LastModifiedDate = baseDate
        };
        header.GenerateToolListName();

        var seedEntry = GetToolListDetailsSeedData().FirstOrDefault(x => x.PartNumber == partNumber && x.Operation == operation);
        var consumableCodes = seedEntry.ConsumableCodes ?? Array.Empty<string>();
        header.Details = consumableCodes
            .Select((code, i) =>
            {
                if (!masterLookup.TryGetValue(code, out var m)) return null;
                return new ToolListDetail
                {
                    ToolNumber = $"T{(i + 1):D2}",
                    ConsumableCode = code,
                    ToolDescription = m.SystemToolName,
                    Supplier = m.Supplier,
                    Diameter = m.Dia,
                    FluteLength = m.Flute,
                    ProtrusionLength = 45.0m,
                    CornerRadius = m.Radius,
                    HolderExtensionCode = "ER32",
                    ArborCode = "BT40-ER32",
                    ToolPathTimeMinutes = 0,
                    Remarks = ""
                };
            })
            .Where(d => d != null)
            .Cast<ToolListDetail>()
            .ToList();
        return header;
    }
    
    private static void UpdateToolMaster(ApplicationDbContext context, ToolListDetail detail)
    {
        if (string.IsNullOrWhiteSpace(detail.ConsumableCode))
            return;
            
        var existing = context.ToolMasters.FirstOrDefault(t => t.ConsumableCode == detail.ConsumableCode);
        if (existing == null)
        {
            context.ToolMasters.Add(new ToolMaster
            {
                ConsumableCode = detail.ConsumableCode,
                ToolDescription = detail.ToolDescription,
                Supplier = detail.Supplier,
                Diameter = detail.Diameter,
                FluteLength = detail.FluteLength,
                ProtrusionLength = detail.ProtrusionLength,
                CornerRadius = detail.CornerRadius,
                HolderExtensionCode = detail.HolderExtensionCode,
                ArborCode = detail.ArborCode,
                LastUpdated = DateTime.UtcNow
            });
        }
        else
        {
            existing.ToolDescription = detail.ToolDescription;
            existing.Supplier = detail.Supplier;
            existing.Diameter = detail.Diameter;
            existing.FluteLength = detail.FluteLength;
            existing.ProtrusionLength = detail.ProtrusionLength;
            existing.CornerRadius = detail.CornerRadius;
            existing.HolderExtensionCode = detail.HolderExtensionCode;
            existing.ArborCode = detail.ArborCode;
            existing.LastUpdated = DateTime.UtcNow;
        }
    }

    // ---- Reset from seed (used by Settings Reset buttons) ----

    public static void ResetUsers(ApplicationDbContext context)
    {
        context.Users.RemoveRange(context.Users.ToList());
        context.SaveChanges();
        var userMasterPath = ResolveUserMasterPath();
        if (string.IsNullOrEmpty(userMasterPath))
            throw new InvalidOperationException("Missing file: USER MASTER.xlsx not found. Place the file in the Data folder.");
        var userRows = LoadUsersFromExcel(userMasterPath);
        if (userRows.Count == 0)
            throw new InvalidOperationException("No data loaded: USER MASTER.xlsx is empty or column headers do not match. Expected: Username, Password, Display Name; optional: Status.");
        foreach (var (username, password, displayName, isActive) in userRows)
        {
            context.Users.Add(new User { Username = username, Password = password, DisplayName = displayName, IsActive = isActive, CreatedDate = DateTime.UtcNow });
        }
        context.SaveChanges();
    }

    public static void ResetProjectCodes(ApplicationDbContext context)
    {
        if (context.ProjectCodes == null) return;
        // Null FK references first to avoid constraint issues when deleting project codes
        if (context.PartNumbers != null)
        {
            foreach (var pn in context.PartNumbers.Where(p => p.ProjectCodeId != null))
                pn.ProjectCodeId = null;
            context.SaveChanges();
        }
        context.ProjectCodes.RemoveRange(context.ProjectCodes.ToList());
        context.SaveChanges();
        foreach (var (code, description, project, isActive) in GetProjectCodeSeedData())
        {
            context.ProjectCodes.Add(new ProjectCode { Code = code, Description = description, Project = project, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
        }
        context.SaveChanges();
    }

    public static void ResetMachineNames(ApplicationDbContext context)
    {
        if (context.MachineNames == null) return;
        context.MachineNames.RemoveRange(context.MachineNames.ToList());
        context.SaveChanges();
        var modelToId = (context.MachineModels ?? Enumerable.Empty<MachineModel>())
            .ToDictionary(m => m.Model, m => m.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, serial, workcenter, machineModel, isActive) in GetMachineNameSeedData())
        {
            var machineModelId = !string.IsNullOrWhiteSpace(machineModel) && modelToId.TryGetValue(machineModel.Trim(), out var id) ? id : (int?)null;
            context.MachineNames.Add(new MachineName { Name = name, Description = serial, Workcenter = workcenter ?? "", MachineModelId = machineModelId, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
        }
        context.SaveChanges();
    }

    public static void ResetMachineWorkcenters(ApplicationDbContext context)
    {
        if (context.MachineWorkcenters == null) return;
        context.MachineWorkcenters.RemoveRange(context.MachineWorkcenters.ToList());
        context.SaveChanges();
        foreach (var (workcenter, axis, isActive) in GetMachineWorkcenterSeedData())
        {
            context.MachineWorkcenters.Add(new MachineWorkcenter { Workcenter = workcenter, Axis = axis, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
        }
        context.SaveChanges();
    }

    public static void ResetMachineModels(ApplicationDbContext context)
    {
        if (context.MachineModels == null) return;
        context.MachineModels.RemoveRange(context.MachineModels.ToList());
        context.SaveChanges();
        foreach (var (model, description, type, controller, isActive) in GetMachineModelSeedData())
        {
            context.MachineModels.Add(new MachineModel
            {
                Model = model,
                Description = description,
                Type = type,
                Controller = controller,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "system",
                IsActive = isActive
            });
        }
        context.SaveChanges();
    }

    public static void ResetCamLeaders(ApplicationDbContext context)
    {
        if (context.CamLeaders == null) return;
        context.CamLeaders.RemoveRange(context.CamLeaders.ToList());
        context.SaveChanges();
        var path = ResolveCamLeaderPath();
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("Missing file: CAM LEADER - MASTER.xlsx not found. Place the file in the Data folder.");
        var rows = LoadCamLeaderFromExcel(path);
        if (rows.Count == 0)
            throw new InvalidOperationException("No data loaded: CAM LEADER - MASTER.xlsx is empty or column headers do not match. Expected: Name, Position; optional: Status (ACTIVE/INACTIVE).");
        foreach (var (name, position, isActive) in rows)
        {
            context.CamLeaders.Add(new CamLeader { Name = name, Description = position ?? "", CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
        }
        context.SaveChanges();
    }

    public static void ResetCamProgrammers(ApplicationDbContext context)
    {
        if (context.CamProgrammers == null) return;
        context.CamProgrammers.RemoveRange(context.CamProgrammers.ToList());
        context.SaveChanges();
        var path = ResolveCamProgrammerPath();
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("Missing file: CAM PROGRAMMER - MASTER.xlsx not found. Place the file in the Data folder.");
        var rows = LoadCamProgrammerFromExcel(path);
        if (rows.Count == 0)
            throw new InvalidOperationException("No data loaded: CAM PROGRAMMER - MASTER.xlsx is empty or column headers do not match. Expected: Name, Location; optional: Status (ACTIVE/INACTIVE).");
        foreach (var (name, location, isActive) in rows)
        {
            context.CamProgrammers.Add(new CamProgrammer { Name = name, Description = location ?? "", CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
        }
        context.SaveChanges();
    }

    public static void ResetOperations(ApplicationDbContext context)
    {
        if (context.Operations == null) return;
        context.Operations.RemoveRange(context.Operations.ToList());
        context.SaveChanges();
        foreach (var name in new[] { "OP10", "OP20", "OP30", "OP40", "OP50", "OP60", "OP70", "OP80", "OP90" })
            context.Operations.Add(new Operation { Name = name, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        context.SaveChanges();
    }

    public static void ResetRevisions(ApplicationDbContext context)
    {
        if (context.Revisions == null) return;
        context.Revisions.RemoveRange(context.Revisions.ToList());
        context.SaveChanges();
        foreach (var name in new[] { "REV00", "REV01", "REV02", "REV03", "REV04", "REV05", "REV06", "REV07", "REV08", "REV09", "REV10" })
            context.Revisions.Add(new Revision { Name = name, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        context.SaveChanges();
    }

    public static void ResetMaterialSpecs(ApplicationDbContext context)
    {
        if (context.MaterialSpecs == null) return;
        if (context.PartNumbers != null)
        {
            foreach (var pn in context.PartNumbers.Where(p => p.MaterialSpecId != null))
                pn.MaterialSpecId = null;
            context.SaveChanges();
        }
        context.MaterialSpecs.RemoveRange(context.MaterialSpecs.ToList());
        context.SaveChanges();
        var excelPath = ResolveMaterialSpecMasterPath();
        if (string.IsNullOrEmpty(excelPath))
            throw new InvalidOperationException("Missing file: MATERIAL SPEC MASTER.xlsx not found. Place the file in the Data folder.");
        var rows = LoadMaterialSpecFromExcel(excelPath);
        if (rows.Count == 0)
            throw new InvalidOperationException("No data loaded: MATERIAL SPEC MASTER.xlsx is empty or column headers do not match. Expected headers: Material Specification (On Drawing), General Name; optional: Material Specification (Purchased), Material Supply Condition (Purchased), Material Type, Status.");
        var seen = new HashSet<(string, string)>();
        foreach (var (spec, specPurchased, material, supplyCondition, materialType, isActive) in rows)
        {
            var key = ((spec ?? "").ToLowerInvariant(), (material ?? "").ToLowerInvariant());
            if (seen.Contains(key)) continue;
            seen.Add(key);
            context.MaterialSpecs.Add(new MaterialSpec
            {
                Spec = spec ?? "",
                MaterialSpecPurchased = specPurchased ?? "",
                Material = material ?? "",
                MaterialSupplyConditionPurchased = supplyCondition ?? "",
                MaterialType = materialType ?? "",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "system",
                IsActive = isActive
            });
        }
        context.SaveChanges();
    }

    public static void ResetToolSuppliers(ApplicationDbContext context)
    {
        if (context.ToolSuppliers == null) return;
        context.ToolSuppliers.RemoveRange(context.ToolSuppliers.ToList());
        context.SaveChanges();
        var excelPath = ResolveToolSupplierMasterPath();
        if (string.IsNullOrEmpty(excelPath))
            throw new InvalidOperationException("Missing file: TOOL SUPPLIER MASTER.xlsx not found. Place the file in the Data folder.");
        var rows = LoadToolSupplierFromExcel(excelPath);
        if (rows.Count == 0)
            throw new InvalidOperationException("No data loaded: TOOL SUPPLIER MASTER.xlsx is empty or column headers do not match. Expected: Supplier or Tool Supplier or Name; Website; Status.");
        foreach (var (name, website, status) in rows)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            context.ToolSuppliers.Add(new ToolSupplier
            {
                Name = name ?? "",
                Website = website,
                Status = status ?? "",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "system"
            });
        }
        context.SaveChanges();
    }

    public static void ResetPartNumbers(ApplicationDbContext context)
    {
        if (context.PartNumbers == null) return;
        context.PartNumbers.RemoveRange(context.PartNumbers.ToList());
        context.SaveChanges();
        var projectCodes = (context.ProjectCodes ?? Enumerable.Empty<ProjectCode>()).ToDictionary(p => p.Code, p => p.Id);
        var materialSpecs = (context.MaterialSpecs ?? Enumerable.Empty<MaterialSpec>()).ToList();
        var matBySpec = materialSpecs.GroupBy(m => m.Spec).ToDictionary(g => g.Key, g => g.First().Id);
        foreach (var (name, desc, partRev, drawRev, pcCode, refDrawing, msSpec, sequence) in GetPartNumberSeedData())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var pcId = !string.IsNullOrEmpty(pcCode) && projectCodes.TryGetValue(pcCode, out var pid) ? pid : (int?)null;
            var msId = !string.IsNullOrEmpty(msSpec) && matBySpec.TryGetValue(msSpec, out var mid) ? mid : (int?)null;
            context.PartNumbers.Add(new PartNumber { Name = name, Description = desc, ProjectCodeId = pcId, PartRev = partRev, DrawingRev = drawRev, MaterialSpecId = msId, RefDrawing = refDrawing ?? "", Sequence = sequence, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        }
        context.SaveChanges();
    }

    /// <summary>Load Master Tool Code rows from TOOL CODE MASTER.xlsx. Columns: System Tool Name, Tool Description, Procurement channel, Tool Ø (DC), Flute / Cutting edge length (APMXS) cutting width (CW), Corner rad.</summary>
    private static List<(string SystemToolName, string ConsumableCode, string Supplier, decimal Diameter, decimal FluteLength, decimal CornerRadius)> LoadToolCodeUniqueFromExcel(string path)
    {
        var result = new List<(string, string, string, decimal, decimal, decimal)>();
        if (!File.Exists(path)) return result;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(new FileInfo(path));
        var ws = package.Workbook.Worksheets.FirstOrDefault();
        if (ws?.Dimension == null) return result;
        int cols = ws.Dimension.End.Column;
        int rows = ws.Dimension.End.Row;
        if (rows < 2) return result;
        static int GetCol(ExcelWorksheet sheet, int totalCols, string headerName)
        {
            for (int c = 1; c <= totalCols; c++)
            {
                var v = sheet.Cells[1, c].Value?.ToString()?.Trim();
                if (string.Equals(v, headerName, StringComparison.OrdinalIgnoreCase)) return c;
            }
            return -1;
        }
        int colSystemToolName = GetCol(ws, cols, "System Tool Name");
        int colToolDescription = GetCol(ws, cols, "Tool Description");
        int colProcurementChannel = GetCol(ws, cols, "Procurement channel");
        int colToolDiameter = GetCol(ws, cols, "Tool Ø (DC)");
        int colFluteLength = GetCol(ws, cols, "Flute / Cutting edge length (APMXS) cutting width (CW)");
        int colCornerRad = GetCol(ws, cols, "Corner rad");
        if (colSystemToolName < 0 && colToolDescription < 0) return result;
        static decimal ParseDecimal(ExcelWorksheet sheet, int row, int col)
        {
            if (col < 1) return 0;
            var v = sheet.Cells[row, col].Value;
            if (v == null) return 0;
            if (v is double d) return (decimal)d;
            if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec)) return dec;
            return 0;
        }
        static string GetStr(ExcelWorksheet sheet, int row, int col) => col >= 1 ? sheet.Cells[row, col].Value?.ToString()?.Trim() ?? "" : "";
        for (int r = 2; r <= rows; r++)
        {
            var systemName = GetStr(ws, r, colSystemToolName);
            var consumable = GetStr(ws, r, colToolDescription);
            var supplier = GetStr(ws, r, colProcurementChannel);
            var diameter = ParseDecimal(ws, r, colToolDiameter);
            var flute = ParseDecimal(ws, r, colFluteLength);
            var radius = ParseDecimal(ws, r, colCornerRad);
            if (string.IsNullOrWhiteSpace(systemName) && string.IsNullOrWhiteSpace(consumable)) continue;
            result.Add((systemName ?? "", consumable ?? "", supplier ?? "", diameter, flute, radius));
        }
        return result;
    }

    public static void ResetToolCodeUniques(ApplicationDbContext context)
    {
        if (context.ToolCodeUniques == null) return;
        var conn = context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var dbTrans = conn.BeginTransaction();
        try
        {
            using (var delCmd = conn.CreateCommand())
            {
                delCmd.Transaction = dbTrans;
                delCmd.CommandText = "DELETE FROM ToolCodeUniques";
                delCmd.ExecuteNonQuery();
            }
            using (var seqCmd = conn.CreateCommand())
            {
                seqCmd.Transaction = dbTrans;
                seqCmd.CommandText = "DELETE FROM sqlite_sequence WHERE name='ToolCodeUniques'";
                seqCmd.ExecuteNonQuery();
            }
            const string insertSql = @"INSERT INTO ToolCodeUniques (SystemToolName, ConsumableCode, Supplier, Diameter, FluteLength, CornerRadius, CreatedDate, LastModifiedDate)
                VALUES (@sn, @cc, @su, @di, @fl, @cr, @cd, @lm)";
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var excelPath = Path.Combine(AppContext.BaseDirectory, "Data", "TOOL CODE MASTER.xlsx");
            var rowsToInsert = LoadToolCodeUniqueFromExcel(excelPath);
            foreach (var (systemName, consumable, supplier, dia, flute, radius) in rowsToInsert)
            {
                using var insCmd = conn.CreateCommand();
                insCmd.Transaction = dbTrans;
                insCmd.CommandText = insertSql;
                AddParam(insCmd, "@sn", systemName);
                AddParam(insCmd, "@cc", consumable);
                AddParam(insCmd, "@su", supplier);
                AddParam(insCmd, "@di", dia);
                AddParam(insCmd, "@fl", flute);
                AddParam(insCmd, "@cr", radius);
                AddParam(insCmd, "@cd", baseTime.ToString("o", CultureInfo.InvariantCulture));
                AddParam(insCmd, "@lm", baseTime.ToString("o", CultureInfo.InvariantCulture));
                insCmd.ExecuteNonQuery();
            }
            dbTrans.Commit();
        }
        catch
        {
            dbTrans.Rollback();
            throw;
        }
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    public static void ResetToolLists(ApplicationDbContext context)
    {
        context.ToolListHeaders.RemoveRange(context.ToolListHeaders.ToList());
        context.SaveChanges();
        var excelPath = Path.Combine(AppContext.BaseDirectory, "Data", "TOOL CODE MASTER.xlsx");
        var toolCodeList = LoadToolCodeUniqueFromExcel(excelPath);
        // Group by ConsumableCode and take first row per key so duplicate codes in Excel do not throw
        var masterLookup = toolCodeList
            .GroupBy(t => t.ConsumableCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => { var f = g.First(); return (f.SystemToolName, f.Supplier, f.Diameter, f.FluteLength, f.CornerRadius); }, StringComparer.OrdinalIgnoreCase);
        var toolLists = new List<ToolListHeader>
        {
            CreateToolListWithDetails("V5754221420001", "OP10", "REV00", "AG01", "S001", "2X-01", "DMU50", "hakim.hisham", masterLookup),
            CreateToolListWithDetails("V5754221420001", "OP20", "REV00", "AG01", "S001", "2X-01", "DMU50", "hakim.hisham", masterLookup),
            CreateToolListWithDetails("351-2180-7", "OP10", "REV00", "AG02", "SP11", "5X-01", "VCN510C", "adib.jamil", masterLookup),
            CreateToolListWithDetails("E5757332620000", "OP10", "REV00", "AL01", "K5-42", "3X-07", "Integrex i-200", "faiq.faizul", masterLookup),
            CreateToolListWithDetails("E5757332620000", "OP20", "REV00", "AL01", "K5-42", "3X-07", "Integrex i-200", "faiq.faizul", masterLookup),
        };
        context.ToolListHeaders.AddRange(toolLists);
        context.SaveChanges();
        var processedCodes = new HashSet<string>();
        foreach (var header in toolLists)
        {
            foreach (var detail in header.Details)
            {
                if (!string.IsNullOrWhiteSpace(detail.ConsumableCode) && !processedCodes.Contains(detail.ConsumableCode))
                {
                    UpdateToolMaster(context, detail);
                    processedCodes.Add(detail.ConsumableCode);
                    context.SaveChanges();
                }
            }
        }
    }
}
