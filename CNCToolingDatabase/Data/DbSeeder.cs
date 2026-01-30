using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CNCToolingDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
                    CREATE INDEX IF NOT EXISTS IX_PartNumbers_Name ON PartNumbers(Name);
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_MaterialSpecs_Spec_Material ON MaterialSpecs(Spec, Material);
                ";
                command.ExecuteNonQuery();
                try { command.CommandText = "ALTER TABLE ProjectCodes ADD COLUMN Project TEXT;"; command.ExecuteNonQuery(); } catch { /* column may exist */ }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN ProjectCodeId INTEGER REFERENCES ProjectCodes(Id);"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN PartRev TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN DrawingRev TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN MaterialSpecId INTEGER REFERENCES MaterialSpecs(Id);"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE PartNumbers ADD COLUMN RefDrawing TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE MachineModels ADD COLUMN Type TEXT;"; command.ExecuteNonQuery(); } catch { }
                try { command.CommandText = "ALTER TABLE MachineModels ADD COLUMN Controller TEXT;"; command.ExecuteNonQuery(); } catch { }
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
        
        // Seed settings tables only when empty; never wipe existing data
        if (!context.Users.Any())
        {
            foreach (var (displayName, username, password) in new[] { ("Adib Jamil", "adib.jamil", "123"), ("Bakhari Hussin", "bakhari.hussin", "123"), ("Faiq Faizul", "faiq.faizul", "123"), ("Hakim Hisham", "hakim.hisham", "123"), ("Hakim Ramaly", "hakim.ramaly", "123"), ("Ismail Jahrin", "ismail.jahrin", "123"), ("Low Boon Bao", "boon.bao", "123"), ("Nik Faiszal Abdullah", "nik.faiszal", "123"), ("Tan Chee Wei", "chee.wei", "123") })
            {
                context.Users.Add(new User { Username = username, Password = password, DisplayName = displayName });
            }
            context.SaveChanges();
        }
        
        try
        {
            if (context.ProjectCodes != null && !context.ProjectCodes.Any())
            {
                var codeCustomerProjectTrios = new[] {
                    ("AB03", "UTAS US", "UTAS A350 Component"),
                    ("AD01", "Senior Ermeto", "MCS-C"),
                    ("AD02", "Meggitt Akrons Braking Systems", "MSS-F"),
                    ("AD03", "UTAS India", "MABS A"),
                    ("AE01", "Celestica", "Honeywell"),
                    ("AE02", "Honeywell", "Celestica Localisation"),
                    ("AE03", "Spirit Aerosystems", "DU1080"),
                    ("AE04", "Celestica", "Plexus"),
                    ("AE05", "Airbus Atlantic", "VMI PULL"),
                    ("AG01", "Honeywell", "A350 XWB"),
                    ("AG02", "SOGERMA, FRENCH", "A320 FTB-6B"),
                    ("AG03", "Meggitt Coventry", "Spirit Subang A320 6C"),
                    ("AG04", "Meggitt Akrons Braking Systems", "SPIRIT EPIPHRON TTI & CR"),
                    ("AG05", "UTAS India", "Spirit Wave 4"),
                    ("AG06", "Airbus", "WAVE 5"),
                    ("AG07", "Airbus Atlantic", "A321 XLR"),
                    ("AH01", "Meggitt Coventry", "Goodrich"),
                    ("AH02", "SAM", "787 Fan Cowl"),
                    ("AH03", "UTAS / CTRM", "A350 Fan Cowl"),
                    ("AH04", "UTAS US", "Motor Controller Plates"),
                    ("AH05", "UTAS US", "C-Series & MRJ Fan Cowl"),
                    ("AJ01", "Meggitt Coventry", "Celestica HS"),
                    ("AL01", "Meggitt Akrons Braking Systems", "A350 SOGERMA"),
                    ("AL02", "SOGERMA, FRENCH", "A321 S14A SOGERMA"),
                    ("AL03", "UTAS / CTRM", "Celeste Seat"),
                    ("AL04", "GKN Aerospace", "A350 MLGB"),
                    ("AL05", "Meggitt Coventry", "Celeste Seat 2nd Package"),
                    ("AL06", "Honeywell", "AIRBUS D2P SBMSA"),
                    ("AL07", "GKN Aerospace", "STELIA D2P SBMSA"),
                    ("AL10", "Airbus", "Airbus Atlantic NPI & SB76"),
                    ("AM01", "Celestica", "MC130 & MC133"),
                    ("AM03", "UTAS / CTRM", "GKN A350 TE"),
                    ("AP02", "SAM", "FHI-SAT Fitting"),
                    ("SA01", "Stelia Aerospace", "SSP A320 Neo & C-Series Flanges"),
                    ("SB01", "Spirit Aerosystems", "Senior Ermeto")
                };
                foreach (var (code, customer, project) in codeCustomerProjectTrios)
                {
                    context.ProjectCodes.Add(new ProjectCode { Code = code, Description = customer, Project = project, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
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
                foreach (var (name, serial, isActive) in GetMachineNameSeedData())
                {
                    context.MachineNames.Add(new MachineName
                    {
                        Name = name,
                        Description = serial,
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
                var workcenters = "2X-01|2X-02|2X-03|2X-04|2X-06|2X-07|2X-08|2X-09|2X-10|2X-11|3X-01|3X-02|3X-03|3X-07|3X-08|3X-09|3X-09i|3X-10|3X-11|3X-14|3X-18|3X-19|3X-20|3X-21|3X-22|3X-23|3X-26|3X-27|3X-28|3X-29|3X-30|3X-31|3X-32|4X-01|4X-02|4X-03|4X-07|4X-08|4X-10|4X-11|4X-13|4X-14|4X-15|4X-16|5X-01|5X-02|5X-03|5X-04|5X-05|5X-06|5X-07|5X-08|5X-09|5X-10|5X-11|5X-12|5X-13|5X-14|5X-15".Split('|');
                foreach (var wc in workcenters)
                {
                    var axis = wc.StartsWith("2X") ? "2-Axis" : wc.StartsWith("3X") ? "3-Axis" : wc.StartsWith("4X") ? "4-Axis" : wc.StartsWith("5X") ? "5-Axis" : "3-Axis";
                    context.MachineWorkcenters.Add(new MachineWorkcenter { Workcenter = wc, Description = axis, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MachineModels != null && !context.MachineModels.Any())
            {
                var machineModelSeed = new[] {
                    ("AERO-426", "Hartford", "Heidenhain", "Mill"),
                    ("CMX1100V", "DMG", "Fanuc", "Mill"),
                    ("DMC1150V", "DMG", "Heidenhain", "Mill"),
                    ("DMC60H", "DMG", "Heidenhain", "Mill"),
                    ("DMC80 U duoBLOCK", "DMG", "Heidenhain", "Mill"),
                    ("DMU50", "DMG", "Heidenhain", "Mill"),
                    ("DMU60 Evo", "DMG", "Heidenhain", "Mill"),
                    ("DMU65 monoBLOCK", "DMG", "Heidenhain", "Mill"),
                    ("DMU95 monoBLOCK", "DMG", "Heidenhain", "Mill"),
                    ("DNM500HS", "Doosan", "Fanuc", "Mill"),
                    ("DNM750L-II", "Doosan", "Fanuc", "Mill"),
                    ("E17040-V2", "Ares Seiki", "Siemens", "Mill"),
                    ("FANUC Robodrill a-T21iFb", "Fanuc", "Fanuc", "Mill"),
                    ("DVD5200 Dual Spindle", "FFG DMC", "Fanuc", "Mill"),
                    ("HCN4000 II", "Mazak", "Mazatrol", "Mill"),
                    ("HCN4000 III", "Mazak", "Mazatrol", "Mill"),
                    ("HCN6000 C", "Mazak", "Mazatrol", "Mill"),
                    ("HCN6000 II", "Mazak", "Mazatrol", "Mill"),
                    ("HiREX-4000", "Hwacheon", "Fanuc", "Mill"),
                    ("HTC 4000-II", "Mazak", "Mazatrol", "Mill"),
                    ("Integrex i-200", "Mazak", "Mazatrol", "MillTurn"),
                    ("Integrex i-630V", "Mazak", "Mazatrol", "MillTurn"),
                    ("Integrex j-200", "Mazak", "Mazatrol", "MillTurn"),
                    ("Integrex j-200s", "Mazak", "Mazatrol", "MillTurn"),
                    ("MYNX 9500", "Doosan", "Heidenhain", "Mill"),
                    ("NLX1500", "DMG", "Celos", "MillTurn"),
                    ("NVX5060", "DMG", "Celos", "Mill"),
                    ("NVX5060 HT", "DMG", "Celos", "Mill"),
                    ("NVX5100 (3X + Indexer)", "DMG", "Celos", "Mill"),
                    ("NVX7000 (3X + Indexer)", "DMG", "Celos", "Mill"),
                    ("PFH4800", "Mazak", "Mazatrol", "Mill"),
                    ("QT200", "Mazak", "Mazatrol", "Lathe"),
                    ("QTC200MSY L", "Mazak", "Mazatrol", "Lathe"),
                    ("QTE200", "Mazak", "Mazatrol", "Lathe"),
                    ("QTN100", "Mazak", "Mazatrol", "Lathe"),
                    ("QTN100-II MSY", "Mazak", "Mazatrol", "Lathe"),
                    ("QTN150", "Mazak", "Mazatrol", "Lathe"),
                    ("QTN200", "Mazak", "Mazatrol", "Lathe"),
                    ("SIRIUS-650", "Hwacheon", "Fanuc", "Mill"),
                    ("TMV1600A", "TongTai", "Fanuc", "Mill"),
                    ("TMV1600A (Indexer)", "TongTai", "Fanuc", "Mill"),
                    ("TMV510A-II (Indexer)", "TongTai", "Fanuc", "Mill"),
                    ("TMV1500A (3X + Indexer)", "TongTai", "Fanuc", "Mill"),
                    ("Tornos Delta 38-5A", "Tornos", "Fanuc", "Lathe"),
                    ("UM Dual Spindle Machine", "UGINT", "Mitsubishi", "Mill"),
                    ("UM500DH", "UGINT", "Mitsubishi", "Mill"),
                    ("UM500DH (3X + Indexer)", "UGINT", "Mitsubishi", "Mill"),
                    ("UM-V500", "UGINT", "Mitsubishi", "Mill"),
                    ("VCN410A", "Mazak", "Mazatrol", "Mill"),
                    ("VCN410A Indexer", "Mazak", "Mazatrol", "Mill"),
                    ("VCN410A-II", "Mazak", "Mazatrol", "Mill"),
                    ("VCN430A-II HS", "Mazak", "Mazatrol", "Mill"),
                    ("VCN510C", "Mazak", "Mazatrol", "Mill"),
                    ("VCN510C-II", "Mazak", "Mazatrol", "Mill"),
                    ("VCN515C", "Mazak", "Mazatrol", "Mill"),
                    ("VCN530C-HS (3X + Indexer)", "Mazak", "Mazatrol", "Mill"),
                    ("VCN535", "Mazak", "Mazatrol", "Mill"),
                    ("VCN700D (3X + Indexer)", "Mazak", "Mazatrol", "Mill"),
                    ("VCS430A", "Mazak", "Mazatrol", "Mill"),
                    ("Victor Turning", "Victor", "Fanuc", "Lathe"),
                    ("Vortex i-630V/6", "Mazak", "Mazatrol", "Mill"),
                    ("VRX500", "Mazak", "Mazatrol", "Mill"),
                    ("VRX730-5X II", "Mazak", "Mazatrol", "Mill"),
                    ("VRXi-500", "Mazak", "Mazatrol", "Mill"),
                    ("VTC200C", "Mazak", "Mazatrol", "Mill"),
                };
                foreach (var (model, builder, controller, type) in machineModelSeed)
                {
                    context.MachineModels.Add(new MachineModel
                    {
                        Model = model,
                        Description = builder,
                        Type = type,
                        Controller = controller,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = "system",
                        IsActive = true
                    });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.CamLeaders != null && !context.CamLeaders.Any())
            {
                var camLeaderSeed = new[] { ("Venkatesan", "Director"), ("Desmond", "HOD"), ("Adib Jamil", "CAM Manager") };
                foreach (var (name, position) in camLeaderSeed)
                {
                    context.CamLeaders.Add(new CamLeader { Name = name, Description = position, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.CamProgrammers != null && !context.CamProgrammers.Any())
            {
                var camProgrammerSeed = new[] { ("Adib Jamil", "Subang Plant"), ("Bakhari Hussin", "Shah Alam Plant"), ("Faiq Faizul", "Subang Plant"), ("Hakim Hisham", "Subang Plant"), ("Hakim Ramaly", "Shah Alam Plant"), ("Ismail Jahrin", "Subang Plant"), ("Low Boon Bao", "Shah Alam Plant"), ("Nik Faiszal Abdullah", "Subang Plant"), ("Tan Chee Wei", "Shah Alam Plant") };
                foreach (var (name, location) in camProgrammerSeed)
                {
                    context.CamProgrammers.Add(new CamProgrammer { Name = name, Description = location, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MaterialSpecs != null && !context.MaterialSpecs.Any())
            {
                var materialSpecPairs = new[] {
                    ("ABP3-2101", "Aluminum Alloy 7075"),
                    ("ABP3-2304", "Aluminum Alloy 2024"),
                    ("ABP3-4001", "Titanium Alloy Ti-6Al-4V"),
                    ("ABP3-4201", "Titanium Alloy Plate"),
                    ("ABP3-7101", "15-5PH Stainless Steel"),
                    ("BMS7-304", "Aluminum Alloy 7075"),
                    ("BMS7-26", "Aluminum Alloy 2024"),
                    ("AMS4928", "Titanium Alloy Ti-6Al-4V"),
                    ("AMS5643", "Stainless Steel 321"),
                    ("AMS5662", "Inconel 718 Nickel Alloy"),
                    ("BMS7-304", "Aluminum Alloy 7075-T6/T73"),
                    ("BMS7-26", "Aluminum Alloy 2024-T3/T351"),
                    ("BMS7-331", "Titanium Alloy 6AL-4V"),
                    ("BMS7-380", "Stainless Steel 15-5PH"),
                    ("BMS7-430", "Inconel 718 Nickel Alloy"),
                };
                foreach (var (spec, material) in materialSpecPairs)
                {
                    context.MaterialSpecs.Add(new MaterialSpec { Spec = spec, Material = material, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.PartNumbers != null && !context.PartNumbers.Any())
            {
                var projectCodes = (context.ProjectCodes ?? Enumerable.Empty<ProjectCode>()).ToDictionary(p => p.Code, p => p.Id);
                var materialSpecs = (context.MaterialSpecs ?? Enumerable.Empty<MaterialSpec>()).ToList();
                var matBySpec = materialSpecs.GroupBy(m => m.Spec).ToDictionary(g => g.Key, g => g.First().Id);
                var partSeed = new[]
                {
                    ("351-2123-13", "HINGES-LH", "A00", "A00", "V12345", "AG01", "ABP3-2101"),
                    ("351-2123-14", "BRACKET RH", "B00", "B00", "351-2261", "AG02", "ABP3-2304"),
                    ("351-2123-15", "LINK LH", "A", "A", "D123456", "AG03", "ABP3-4001"),
                    ("351-2123-16", "HINGES-RH", "NA", "NA", "V12346", "AH01", "ABP3-7101"),
                    ("351-2123-21", "BRACKET LH", "A00", "B00", "351-2262", "AL01", "BMS7-304"),
                    ("351-2123-22", "LINK RH", "B00", "A00", "D123457", "AL02", "AMS4928"),
                    ("351-2123-23", "FITTING", "A", "NA", "V12347", "AM01", "AMS5643"),
                    ("351-2123-24", "PLATE ASSY", "NA", "A", "351-2263", "AP02", "AMS5662"),
                    ("351-2123-25", "BRACKET ASSY", "A00", "A00", "D123458", "SA01", "BMS7-331"),
                    ("351-2123-26", "HINGES-CENTER", "B00", "B00", "V12348", "SB01", "BMS7-380"),
                    ("351-2123-27", "LINK ASSY", "A", "A", "351-2264", "AG04", "BMS7-430"),
                    ("351-2123-29", "FITTING LH", "NA", "NA", "D123459", "AE01", "ABP3-2304"),
                };
                foreach (var (name, desc, partRev, drawRev, refDraw, pcCode, msSpec) in partSeed)
                {
                    var pcId = projectCodes.TryGetValue(pcCode, out var pid) ? pid : (int?)null;
                    var msId = matBySpec.TryGetValue(msSpec, out var mid) ? mid : (int?)null;
                    context.PartNumbers.Add(new PartNumber
                    {
                        Name = name,
                        Description = desc,
                        ProjectCodeId = pcId,
                        PartRev = partRev,
                        DrawingRev = drawRev,
                        MaterialSpecId = msId,
                        RefDrawing = refDraw,
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
            var toolLists = new List<ToolListHeader>
            {
                CreateToolListWithDetails("PART-001", "OP10", "REV00", "AG01", "S001", "2X-01", "DMU50", "hakim.hisham"),
                CreateToolListWithDetails("PART-001", "OP20", "REV00", "AG01", "S001", "2X-01", "DMU50", "hakim.hisham"),
                CreateToolListWithDetails("PART-002", "OP10", "REV00", "AG02", "SP11", "5X-01", "VCN510C", "adib.jamil"),
                CreateToolListWithDetails("PART-003", "OP10", "REV00", "AL01", "K5-42", "3X-07", "Integrex i-200", "faiq.faizul"),
                CreateToolListWithDetails("PART-003", "OP20", "REV00", "AL01", "K5-42", "3X-07", "Integrex i-200", "faiq.faizul"),
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
            var baseTime = DateTime.UtcNow.AddDays(-60);
            foreach (var (consumable, supplier, dia, flute, radius) in GetToolCodeUniqueSeedData())
            {
                var systemName = DeriveSystemToolName(consumable, dia, radius);
                var created = baseTime.AddDays(Random.Shared.Next(0, 50));
                var modified = created.AddDays(Random.Shared.Next(0, 20));
                context.ToolCodeUniques.Add(new ToolCodeUnique
                {
                    SystemToolName = systemName,
                    ConsumableCode = consumable,
                    Supplier = supplier,
                    Diameter = dia,
                    FluteLength = flute,
                    CornerRadius = radius,
                    CreatedDate = created,
                    LastModifiedDate = modified
                });
            }
            context.SaveChanges();
        }

        // One-time repopulate SystemToolName for existing ToolCodeUniques (Facemill/Endmill/Drill format)
        const string runOnceKey = "ToolCodeUnique_SystemName_Repopulated";
        if (context.ToolCodeUniques != null && context.ToolCodeUniques.Any())
        {
            try
            {
                var conn = context.Database.GetDbConnection();
                conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM RunOnce WHERE Key = @k";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@k";
                    p.Value = runOnceKey;
                    cmd.Parameters.Add(p);
                    var count = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                    if (count > 0) return;
                }
                finally { conn.Close(); }
                foreach (var t in context.ToolCodeUniques.ToList())
                    t.SystemToolName = DeriveSystemToolName(t.ConsumableCode, t.Diameter, t.CornerRadius);
                context.SaveChanges();
                conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO RunOnce (Key, DoneAt) VALUES (@k, @d)";
                    var pk = cmd.CreateParameter();
                    pk.ParameterName = "@k";
                    pk.Value = runOnceKey;
                    var pd = cmd.CreateParameter();
                    pd.ParameterName = "@d";
                    pd.Value = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    cmd.Parameters.Add(pk);
                    cmd.Parameters.Add(pd);
                    cmd.ExecuteNonQuery();
                }
                finally { conn.Close(); }
            }
            catch
            {
                // RunOnce table may not exist yet; ignore
            }
        }
    }

    /// <summary>
    /// Derives System Tool Name from consumable code and dimensions.
    /// Examples: "Facemill Ø80 R6.0 x 6-Flute", "Endmill Ø16 R0.0 x 3-Flute", "Drill Ø3.2 x 140°".
    /// </summary>
    private static string DeriveSystemToolName(string consumableCode, decimal diameter, decimal cornerRadius)
    {
        var c = consumableCode ?? "";
        var flutes = ParseFlutesFromConsumable(c);
        var isDrill = c.Contains("VXP", StringComparison.OrdinalIgnoreCase);
        var drillAngle = ParseDrillAngleFromConsumable(c);

        if (isDrill)
            return $"Drill Ø{FormatDim(diameter)} x {drillAngle}°";

        var isFacemill = c.Contains("VXD", StringComparison.OrdinalIgnoreCase) || diameter >= 40m;
        var n = flutes > 0 ? flutes : (isFacemill ? 6 : 3);
        var r = FormatDim(cornerRadius);
        return isFacemill
            ? $"Facemill Ø{FormatDim(diameter)} R{r} x {n}-Flute"
            : $"Endmill Ø{FormatDim(diameter)} R{r} x {n}-Flute";
    }

    private static string FormatDim(decimal d)
    {
        return d == Math.Floor(d) ? ((int)d).ToString(CultureInfo.InvariantCulture) : d.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static int ParseFlutesFromConsumable(string c)
    {
        var m = Regex.Match(c, @"Z(\d+)", RegexOptions.IgnoreCase);
        return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : 0;
    }

    private static int ParseDrillAngleFromConsumable(string c)
    {
        if (Regex.IsMatch(c, @"R180(?!\d)|180°")) return 180;
        if (Regex.IsMatch(c, @"R160(?!\d)|160°")) return 160;
        if (Regex.IsMatch(c, @"R140(?!\d)|140°")) return 140;
        if (Regex.IsMatch(c, @"R118(?!\d)|118°")) return 118;
        return 140;
    }

    /// <summary>Machine Name seed: Name|Serial Number|Status. INACTIVE → IsActive false.</summary>
    private static (string Name, string Serial, bool IsActive)[] GetMachineNameSeedData()
    {
        const string raw = @"A002|P117XK535|INACTIVE
A003|G8952-0054|INACTIVE
A004|G8952-0053|INACTIVE
A006|287706|INACTIVE
A007|NL153170920|INACTIVE
A008|G8952-0055|INACTIVE
A009|14585618044|INACTIVE
A010|14585617994|INACTIVE
A011|12620000563|INACTIVE
A012|12620000553|INACTIVE
A013|290299|INACTIVE
A014|G8952-0084|INACTIVE
A015|NL153180416|INACTIVE
A016|294599|INACTIVE
A017|NV504180628|INACTIVE
A018|294600|INACTIVE
A019|12710007873|INACTIVE
A020|NV504180623|INACTIVE
A021|NL153180814|INACTIVE
A022|NV504180626|INACTIVE
A023|NV504180618|INACTIVE
A024|MV0041-000277|INACTIVE
A025|MV0041-000278|INACTIVE
A026|12710007823|INACTIVE
A1-31|MV0097-000172|ACTIVE
A1-33|232129|ACTIVE
A1-52|024433|ACTIVE
A2-31|MV0062-000833|ACTIVE
A2-52|12710007823|ACTIVE
A2-53|10000002906|ACTIVE
A3-31|MV0062-000834|ACTIVE
A3-52|12710008043|ACTIVE
A4-31|G8952-0084|INACTIVE
A4-31a|MV0041-000278|ACTIVE
A4-32|231616|ACTIVE
A4-53|12140018913|ACTIVE
A5-31|G8952-0054|INACTIVE
A5-31A|MV0041-000277|ACTIVE
A5-42|G8952-0055|INACTIVE
A5-42A|266744|ACTIVE
A5-53|12140018903|ACTIVE
A6-31|MV0041-000278|INACTIVE
A6-31a|G8952-0084|ACTIVE
A6-32|MV0041-000277|INACTIVE
A6-32A|G8952-0053|ACTIVE
A6-34|G8952-0054|ACTIVE
A6-35|G8952-0055|ACTIVE
A6-53|12620000553|ACTIVE
A7-31|AZMY4002|ACTIVE
A7-32|AZMY3001|ACTIVE
A7-53|12620000753|ACTIVE
A8-31|AZMY9004|ACTIVE
A8-32|AZMY7003|ACTIVE
A8-53|12620000563|ACTIVE
AV06|P11ZXK017|INACTIVE
AV07|P122XK602|INACTIVE
AV08|P124XKJ69|INACTIVE
AV09|P122XKH81|INACTIVE
AV11|035269|INACTIVE
AV12|035268|INACTIVE
B1-32|203823|ACTIVE
B1-33|P117XK535|INACTIVE
B1-34|034768|ACTIVE
B1-45|034769|ACTIVE
B1-51|12140019183|ACTIVE
B1-53|12140022203|ACTIVE
B2-32|P117XK535|INACTIVE
B2-32a|VD010060|ACTIVE
B2-33|VD010061|ACTIVE
B2-34|VD010067|ACTIVE
B2-35|VD010068|ACTIVE
B2-51|12620000953|ACTIVE
B3-41|268110|ACTIVE
B3-52|15475745484|ACTIVE
BR01|CY-C107021|ACTIVE
C-32|CMXV1231029|INACTIVE
C-33|CMXV1231029|ACTIVE
C-34|CMXV1241101|ACTIVE
C-51|12140022153|ACTIVE
C-52|12140022163|ACTIVE
FC01|243039|ACTIVE
FC02|244490|ACTIVE
FC03|250436|ACTIVE
FC04|250796|ACTIVE
FC05|261526|ACTIVE
FC06|261527|ACTIVE
FC07|11880000913|ACTIVE
FC08|M256363E20A|ACTIVE
FC09|253410|ACTIVE
FC10|265558|ACTIVE
FC11|265559|ACTIVE
FC12|15475739884|ACTIVE
FC13|14585609704|INACTIVE
FC13a|NV505230113|ACTIVE
FC14|14585609674|ACTIVE
FC15|269124|ACTIVE
FC16|14585610364|ACTIVE
FC17|15475739874|ACTIVE
FC18|15475739904|ACTIVE
GK01|034768|INACTIVE
GK02|034769|INACTIVE
GR01|227325|ACTIVE
GR02|203823|INACTIVE
GR03|232129|INACTIVE
GR04|268110|INACTIVE
H1-21|Y2-1286|ACTIVE
H1-42|283496|ACTIVE
H1-43|269004|ACTIVE
H1-44|261684|ACTIVE
H2-31|276775|INACTIVE
H2-51|283192|ACTIVE
H2-52|283193|ACTIVE
H2-53|264750|ACTIVE
H2-54|283194|ACTIVE
H3-31|NV503170603|INACTIVE
H3-34|276775|ACTIVE
H3-35|NV503170512|ACTIVE
H3-36|NV503170603|ACTIVE
H3-51|11415589584|ACTIVE
H3-52|281392|ACTIVE
H3-53|268112|ACTIVE
HW01|183367|INACTIVE
HW02|186970|INACTIVE
HW03|Y2-1286|INACTIVE
HW05|219321|INACTIVE
HW06|219320|INACTIVE
HW07|219322|INACTIVE
HW08|196093|ACTIVE
HW09|220930|INACTIVE
HW12|199831|ACTIVE
HW14|231616|INACTIVE
HW15|231615|INACTIVE
HW16|218349|ACTIVE
HW17|266744|INACTIVE
HW18|266897|INACTIVE
HW19|266896|INACTIVE
HW20|NV503160118|INACTIVE
HW21|NV503160125|INACTIVE
HW22|NV503160116|INACTIVE
HW23|NV503160117|INACTIVE
HW24|G8940-0078|INACTIVE
HW25|NV503160915|INACTIVE
J1-21|219060|ACTIVE
J1-22|233201|ACTIVE
J1-23|212465|ACTIVE
J1-24|218188|ACTIVE
J1-25|217205|ACTIVE
J1-26|178997|ACTIVE
J1-27|178996|ACTIVE
J1-31|G8940-0045|INACTIVE
J1-32|G8940-0078|INACTIVE
J2-21|186970|INACTIVE
J2-22|178996|INACTIVE
J2-31|G8940-0045|ACTIVE
J2-32|220930|ACTIVE
J2-34|G8940-0078|ACTIVE
J2-35|182714|ACTIVE
J2-53|200395|ACTIVE
J2-56|182711|ACTIVE
J3-21|178995|ACTIVE
J3-22|178997|INACTIVE
J4-41|NV504180623|ACTIVE
J4-52|294600|ACTIVE
J5-41|NV504180626|ACTIVE
J5-52|294599|ACTIVE
J6-41|NV504180628|ACTIVE
J6-52|12710007873|ACTIVE
J7-21|290299|INACTIVE
J7-22|287706|INACTIVE
J8-41|NV504180618|ACTIVE
J8-52|311017|ACTIVE
K1-31|035269|INACTIVE
K1-32|035268|INACTIVE
K1-33|232129|INACTIVE
K1-34|231615|ACTIVE
K1-41|266897|ACTIVE
K1-42|266896|ACTIVE
K10-21|NL153180814|ACTIVE
K10-22|NL153170901|ACTIVE
K10-23|NL153180416|ACTIVE
K10-24|NL153170920|ACTIVE
K2-31|P11ZXK017|INACTIVE
K2-32|P122XK602|INACTIVE
K2-34|P11ZXK017|ACTIVE
K2-35|P124XKJ69|ACTIVE
K2-36|P122XKH81|ACTIVE
K2-37|182714|INACTIVE
K2-37a|P117XK535|ACTIVE
K2-41|035268|ACTIVE
K2-42|035269|ACTIVE
K2-43|P122XK602|ACTIVE
K3-31a|14585617994|ACTIVE
K3-32|P124XKJ69|INACTIVE
K3-32a|14585618044|ACTIVE
K3-33|NV505230113|INACTIVE
K3-33a|14585609704|ACTIVE
K3-34|14585613704|ACTIVE
K4-31a|219321|ACTIVE
K4-32|P122XKH81|INACTIVE
K4-33|219322|ACTIVE
K4-34|177480|ACTIVE
K4-35|219320|ACTIVE
K4-42|NV503160915|ACTIVE
K5-21|NL153170901|INACTIVE
K5-22|NL153180814|INACTIVE
K5-41|NV503160117|ACTIVE
K5-42|NV503160116|ACTIVE
K5-43|NV503160125|ACTIVE
K5-44|NV503160118|ACTIVE
K6-21|NL153180416|INACTIVE
K6-21a|NL153170210|ACTIVE
K6-22|NL153170920|INACTIVE
K6-22a|178997|INACTIVE
K6-23|178996|INACTIVE
K7-21|267916|ACTIVE
K7-22|267917|ACTIVE
K7-23|290299|ACTIVE
K7-24|287706|ACTIVE
K7-31|14585618044|INACTIVE
K7-32|14585617994|INACTIVE
K8-21|186970|ACTIVE
K8-22|183367|ACTIVE
K8-23|276379|ACTIVE
K8-24|38510104|ACTIVE
K9-21|NL154230105|ACTIVE
K9-22|335828|ACTIVE
K9-23|NL154230107|ACTIVE
K9-24|335833|ACTIVE
M1-21|219060|INACTIVE
M1-22|218188|INACTIVE
MD01|261684|INACTIVE
MD02|269004|INACTIVE
MD03|268112|INACTIVE
MD04|15475745484|INACTIVE
MD05|264750|INACTIVE
MD06|276775|INACTIVE
MD07|276379|INACTIVE
MD08|NL153170210|INACTIVE
MD09|NL153170210|INACTIVE
MD10|NV503170512|INACTIVE
MD11|NV503170603|INACTIVE
MD12|281392|INACTIVE
MD13|283193|INACTIVE
MD14|283194|INACTIVE
MD15|283192|INACTIVE
S001|286303|ACTIVE
S002|NL153170901|INACTIVE
SM01|178996|INACTIVE
SM02|178995|INACTIVE
SM03|178997|INACTIVE
SM05|182714|INACTIVE
SM06|177480|INACTIVE
SM09|200395|INACTIVE
SM10|182712|ACTIVE
SM11|182711|INACTIVE
SM12|172570|ACTIVE
SM13|183438|ACTIVE
SM14|174085|ACTIVE
SM15|38510104|INACTIVE
SM16|11415589584|INACTIVE
SM17|G8940-0045|INACTIVE
SP00|212465|INACTIVE
SP01|219060|INACTIVE
SP02|218188|INACTIVE
SP03|217205|INACTIVE
SP04|220931|ACTIVE
SP05|196094|ACTIVE
SP06|219355|ACTIVE
SP07|219356|ACTIVE
SP08|198313|ACTIVE
SP09|219339|ACTIVE
SP11|218408|ACTIVE
SP12|218407|ACTIVE
SP13|218406|ACTIVE
SP14|221071|ACTIVE
SP15|230812|ACTIVE
SP16|225314|ACTIVE
SP17|233201|INACTIVE
SP19|266898|ACTIVE
SP20|NV701160110|ACTIVE
SP21|NV701160210|ACTIVE
SP22|275044|ACTIVE
SP23|14585613704|INACTIVE
TM01|267916|INACTIVE
TM02|267917|INACTIVE";
        return raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l =>
            {
                var p = l.Split('|');
                var name = p.Length > 0 ? p[0].Trim() : "";
                var serial = p.Length > 1 ? p[1].Trim() : "";
                var active = p.Length > 2 && string.Equals(p[2].Trim(), "ACTIVE", StringComparison.OrdinalIgnoreCase);
                return (name, serial, active);
            })
            .Where(t => !string.IsNullOrEmpty(t.name))
            .ToArray();
    }

    private static List<(string Consumable, string Supplier, decimal Dia, decimal Flute, decimal Radius)> GetToolCodeUniqueSeedData()
    {
        var data = new List<(string, string, decimal, decimal, decimal)>();
        var pairs = new[]
        {
            ("553120Z3.0-SIRON-A", "SECO", 12.0m, 28.0m, 0m),
            ("553160R050Z3.0-SIRON-A", "SECO", 16.0m, 32.0m, 0.5m),
            ("553080Z3.0-SIRON-A", "SECO", 8.0m, 22.0m, 0m),
            ("553100R250Z3.0-SIRON-A", "SECO", 10.0m, 26.0m, 0.25m),
            ("553100Z3.0-SIRON-A", "SECO", 10.0m, 26.0m, 0m),
            ("A3389DPL-9.8", "WALTER", 9.8m, 24.0m, 0m),
            ("A3389DPL-12", "WALTER", 12.0m, 30.0m, 0m),
            ("A3389AML-2.55", "WALTER", 2.55m, 8.0m, 0m),
            ("A3389DPL-6.1", "WALTER", 6.1m, 18.0m, 0m),
            ("A3389DPL-8.5", "WALTER", 8.5m, 22.0m, 0m),
            ("7792VXP06CA016Z2R140", "KENNAMETAL", 16.0m, 45.0m, 1.4m),
            ("7792VXD09WA032Z3R", "KENNAMETAL", 32.0m, 55.0m, 3.0m),
            ("7792VXD12-A052Z5R", "KENNAMETAL", 52.0m, 65.0m, 5.0m),
            ("7792VXD12-A080Z8R", "KENNAMETAL", 80.0m, 85.0m, 8.0m),
            ("553040Z3.0-SIRON-A", "SECO", 4.0m, 14.0m, 0m),
            ("553060Z3.0-SIRON-A", "SECO", 6.0m, 18.0m, 0m),
            ("553140Z3.0-SIRON-A", "SECO", 14.0m, 32.0m, 0m),
            ("553180R100Z3.0-SIRON-A", "SECO", 18.0m, 38.0m, 1.0m),
            ("553200Z3.0-SIRON-A", "SECO", 20.0m, 42.0m, 0m),
            ("553250Z3.0-SIRON-A", "SECO", 25.0m, 50.0m, 0m),
            ("553120R025Z3.0-SIRON-A", "SECO", 12.0m, 28.0m, 0.25m),
            ("553140R050Z3.0-SIRON-A", "SECO", 14.0m, 32.0m, 0.5m),
            ("553160Z3.0-SIRON-A", "SECO", 16.0m, 36.0m, 0m),
            ("553100R200Z3.0-SIRON-A", "SECO", 10.0m, 26.0m, 0.2m),
            ("553080R050Z3.0-SIRON-A", "SECO", 8.0m, 22.0m, 0.5m),
            ("553220Z3.0-SIRON-A", "SECO", 22.0m, 46.0m, 0m),
            ("553300Z3.0-SIRON-A", "SECO", 30.0m, 58.0m, 0m),
            ("553160R100Z3.0-SIRON-A", "SECO", 16.0m, 36.0m, 1.0m),
            ("553060R025Z3.0-SIRON-A", "SECO", 6.0m, 18.0m, 0.25m),
            ("553140R025Z3.0-SIRON-A", "SECO", 14.0m, 32.0m, 0.25m),
            ("553180Z3.0-SIRON-A", "SECO", 18.0m, 38.0m, 0m),
            ("A3389DPL-10", "WALTER", 10.0m, 26.0m, 0m),
            ("A3389DPL-11", "WALTER", 11.0m, 28.0m, 0m),
            ("A3389AML-3.0", "WALTER", 3.0m, 10.0m, 0m),
            ("A3389AML-4.0", "WALTER", 4.0m, 12.0m, 0m),
            ("A3389DPL-7.0", "WALTER", 7.0m, 20.0m, 0m),
            ("A3389AML-2.0", "WALTER", 2.0m, 6.0m, 0m),
            ("A3389DPL-5.0", "WALTER", 5.0m, 16.0m, 0m),
            ("A3389DPL-14", "WALTER", 14.0m, 34.0m, 0m),
            ("A3389AML-5.0", "WALTER", 5.0m, 14.0m, 0m),
            ("A3389DPL-15", "WALTER", 15.0m, 36.0m, 0m),
            ("7792VXD10-A040Z4R", "KENNAMETAL", 40.0m, 60.0m, 4.0m),
            ("7792VXD16-A100Z10R", "KENNAMETAL", 100.0m, 110.0m, 10.0m),
            ("7792VXP08CA020Z2R160", "KENNAMETAL", 20.0m, 55.0m, 1.6m),
            ("7792VXD12-A063Z6R", "KENNAMETAL", 63.0m, 75.0m, 6.0m),
            ("7792VXD12-A050Z5R", "KENNAMETAL", 50.0m, 62.0m, 5.0m),
            ("7792VXD14-A070Z7R", "KENNAMETAL", 70.0m, 82.0m, 7.0m),
            ("7792VXD08-A032Z3R", "KENNAMETAL", 32.0m, 48.0m, 3.0m),
            ("7792VXP10CA024Z2R180", "KENNAMETAL", 24.0m, 60.0m, 1.8m),
            ("7792VXD20-A120Z12R", "KENNAMETAL", 120.0m, 130.0m, 12.0m),
            ("7792VXD12-A090Z9R", "KENNAMETAL", 90.0m, 100.0m, 9.0m),
        };
        foreach (var t in pairs)
            data.Add(t);
        return data;
    }
    
    private static ToolListHeader CreateToolListWithDetails(string partNumber, string operation, string revision, 
        string projectCode, string machineName, string workcenter, string machineModel, string createdBy)
    {
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
            CreatedDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
            LastModifiedDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 10))
        };
        header.GenerateToolListName();
        
        header.Details = GenerateSampleDetails();
        return header;
    }
    
    private static List<ToolListDetail> GenerateSampleDetails()
    {
        var tools = new List<(string code, string desc, string supplier, decimal dia, decimal flute, decimal protrusion, decimal radius, string holder, string arbor)>
        {
            ("SECO-EM-10", "End Mill 10mm 4 Flute", "Seco", 10.0m, 22.0m, 45.0m, 0.0m, "ER32-10", "BT40-ER32"),
            ("SECO-EM-12", "End Mill 12mm 4 Flute", "Seco", 12.0m, 26.0m, 50.0m, 0.0m, "ER32-12", "BT40-ER32"),
            ("SAND-FM-25", "Face Mill 25mm 5 Insert", "Sandvik", 25.0m, 0.0m, 35.0m, 0.8m, "FM25-A", "BT40-FM25"),
            ("SAND-DR-8", "Drill 8mm Carbide", "Sandvik", 8.0m, 40.0m, 65.0m, 0.0m, "ER25-8", "BT40-ER25"),
            ("WALT-BN-6", "Ball Nose 6mm 2 Flute", "Walter", 6.0m, 12.0m, 30.0m, 3.0m, "ER20-6", "BT40-ER20"),
            ("WALT-CM-16", "Chamfer Mill 16mm 45deg", "Walter", 16.0m, 8.0m, 40.0m, 0.0m, "ER32-16", "BT40-ER32"),
            ("KEN-RP-20", "Roughing End Mill 20mm", "Kennametal", 20.0m, 45.0m, 80.0m, 0.0m, "ER40-20", "BT40-ER40"),
            ("KEN-TH-M10", "Thread Mill M10x1.5", "Kennametal", 8.5m, 15.0m, 35.0m, 0.0m, "ER16-TM", "BT40-ER16"),
            ("SECO-SP-4", "Spot Drill 4mm 90deg", "Seco", 4.0m, 6.0m, 25.0m, 0.0m, "ER16-4", "BT40-ER16"),
            ("SAND-RM-8", "Reamer 8H7", "Sandvik", 8.0m, 25.0m, 50.0m, 0.0m, "ER25-8R", "BT40-ER25"),
        };
        
        var details = new List<ToolListDetail>();
        var toolsToUse = tools.OrderBy(x => Random.Shared.Next()).Take(Random.Shared.Next(3, 8)).ToList();
        
        for (int i = 0; i < toolsToUse.Count; i++)
        {
            var tool = toolsToUse[i];
            details.Add(new ToolListDetail
            {
                ToolNumber = $"T{(i + 1):D2}",
                ConsumableCode = tool.code,
                ToolDescription = tool.desc,
                Supplier = tool.supplier,
                Diameter = tool.dia,
                FluteLength = tool.flute,
                ProtrusionLength = tool.protrusion,
                CornerRadius = tool.radius,
                HolderExtensionCode = tool.holder,
                ArborCode = tool.arbor,
                ToolPathTimeMinutes = 0,
                Remarks = ""
            });
        }
        
        return details;
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
}
