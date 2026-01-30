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
                    ("AB01", "SAM", "Engine Casing"),
                    ("AB02", "SAM", "Panther"),
                    ("AB03", "SAM", "UTAS A350 Component"),
                    ("AD01", "Meggitt Coventry", "MCS-C"),
                    ("AD02", "Meggitt Sensing System", "MSS-F"),
                    ("AD03", "Meggitt Akrons Braking Systems", "MABS A"),
                    ("AE01", "Honeywell", "Honeywell"),
                    ("AE02", "Honeywell", "Celestica Localisation"),
                    ("AE03", "Honeywell", "DU1080"),
                    ("AE04", "Plexus", "Plexus"),
                    ("AE05", "Spirit Aerosystems", "VMI PULL"),
                    ("AG01", "Spirit Aerosystems", "A350 XWB"),
                    ("AG02", "Spirit Aerosystems", "A320 FTB-6B"),
                    ("AG03", "Spirit Aerosystems", "Spirit Subang A320 6C"),
                    ("AG04", "Spirit Aerosystems", "SPIRIT EPIPHRON TTI & CR"),
                    ("AG05", "Spirit Aerosystems", "Spirit Wave 4"),
                    ("AG06", "Spirit Aerosystems", "WAVE 5"),
                    ("AG07", "Spirit Aerosystems", "A321 XLR"),
                    ("AG08", "Spirit Aerosystems", "SPIRIT GOLD"),
                    ("AG09", "Spirit Aerosystems", "SINARAN A320 CA"),
                    ("AH01", "UTAS India", "Goodrich"),
                    ("AH02", "UTAS / CTRM", "787 Fan Cowl"),
                    ("AH03", "UTAS / CTRM", "A350 Fan Cowl"),
                    ("AH04", "UTAS US", "Motor Controller Plates"),
                    ("AH05", "UTAS US", "C-Series & MRJ Fan Cowl"),
                    ("AJ01", "Celestica", "Celestica HS"),
                    ("AL01", "SOGERMA, FRENCH", "A350 SOGERMA"),
                    ("AL02", "SOGERMA, FRENCH", "A321 S14A SOGERMA"),
                    ("AL03", "SOGERMA, FRENCH", "Celeste Seat"),
                    ("AL04", "SOGERMA, FRENCH", "A350 MLGB"),
                    ("AL05", "SOGERMA, FRENCH", "Celeste Seat 2nd Package"),
                    ("AL06", "Stelia Aerospace", "AIRBUS D2P SBMSA"),
                    ("AL07", "Stelia Aerospace", "STELIA D2P SBMSA"),
                    ("AL08", "Senior Aerospace Thailand", "SAT D2P"),
                    ("AL09", "Senior Aerospace Thailand", "SAT D2P"),
                    ("AL10", "Airbus Atlantic", "Airbus Atlantic NPI & SB76"),
                    ("AL11", "Airbus Germany", "AIRBUS GERMANY SA-A350"),
                    ("AL12", "Airbus Germany", "AIRBUS GERMANY SA-A350-KPDD-VAR"),
                    ("AM01", "GKN Aerospace", "MC130 & MC133"),
                    ("AM03", "GKN Aerospace", "GKN A350 TE"),
                    ("AM04", "GKN Aerospace", "GKN A330 BEARING ASSY"),
                    ("AOG", "Airbus", "Airbus"),
                    ("AP02", "Senior Aerospace Thailand", "FHI-SAT Fitting"),
                    ("AQ01", "CTRM Aerosystems", "A350 XWB"),
                    ("SA01", "Senior SSP", "SSP A320 Neo & C-Series Flanges"),
                    ("SB01", "Senior Ermeto", "Senior Ermeto")
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
                foreach (var (name, serial, workcenter, isActive) in GetMachineNameSeedData())
                {
                    context.MachineNames.Add(new MachineName
                    {
                        Name = name,
                        Description = serial,
                        Workcenter = workcenter ?? "",
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
                var workcenters = "2X-01|2X-02|2X-03|2X-04|2X-06|2X-07|2X-08|2X-09|2X-10|2X-11|3X-01|3X-02|3X-03|3X-07|3X-08|3X-09|3X-09i|3X-10|3X-11|3X-14|3X-18|3X-19|3X-20|3X-21|3X-22|3X-23|3X-26|3X-27|3X-28|3X-29|3X-30|3X-31|3X-32|4X-01|4X-02|4X-03|4X-07|4X-08|4X-10|4X-11|4X-13|4X-14|4X-15|4X-16|5X-01|5X-02|5X-03|5X-04|5X-05|5X-06|5X-07|5X-08|5X-09|5X-10|5X-11|5X-12|5X-13|5X-14|5X-15|NA".Split('|');
                foreach (var wc in workcenters)
                {
                    var axis = wc == "NA" ? "N/A" : wc.StartsWith("2X") ? "2-Axis" : wc.StartsWith("3X") ? "3-Axis" : wc.StartsWith("4X") ? "4-Axis" : wc.StartsWith("5X") ? "5-Axis" : "3-Axis";
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

    /// <summary>Machine Name seed: Name|Serial Number|Machine Workcenter|Status. INACTIVE → IsActive false.</summary>
    private static (string Name, string Serial, string Workcenter, bool IsActive)[] GetMachineNameSeedData()
    {
        const string raw = @"A002|P117XK535|3X-09|INACTIVE
A003|G8952-0054|3X-23|INACTIVE
A004|G8952-0053|3X-23|INACTIVE
A006|287706|2X-10|INACTIVE
A007|NL153170920|2X-09|INACTIVE
A008|G8952-0055|3X-23|INACTIVE
A009|14585618044|3X-20|INACTIVE
A010|14585617994|3X-20|INACTIVE
A011|12620000563|5X-12|INACTIVE
A012|12620000553|5X-12|INACTIVE
A013|290299|2X-10|INACTIVE
A014|G8952-0084|3X-23|INACTIVE
A015|NL153180416|2X-09|INACTIVE
A016|294599|5X-08|INACTIVE
A017|NV504180628|4X-10|INACTIVE
A018|294600|5X-08|INACTIVE
A019|12710007873|5X-13|INACTIVE
A020|NV504180623|4X-10|INACTIVE
A021|NL153180814|2X-09|INACTIVE
A022|NV504180626|4X-10|INACTIVE
A023|NV504180618|4X-10|INACTIVE
A024|MV0041-000277|3X-26|INACTIVE
A025|MV0041-000278|3X-26|INACTIVE
A026|12710007823|5X-13|INACTIVE
A1-31|MV0097-000172|3X-27|ACTIVE
A1-33|232129|3X-08|ACTIVE
A1-52|024433|5X-15|ACTIVE
A2-31|MV0062-000833|3X-28|ACTIVE
A2-52|12710007823|5X-13|ACTIVE
A2-53|10000002906|5X-13|ACTIVE
A3-31|MV0062-000834|3X-28|ACTIVE
A3-52|12710008043|5X-13|ACTIVE
A4-31|G8952-0084|3X-23|INACTIVE
A4-31a|MV0041-000278|3X-26|ACTIVE
A4-32|231616|3X-10|ACTIVE
A4-53|12140018913|5X-14|ACTIVE
A5-31|G8952-0054|3X-23|INACTIVE
A5-31A|MV0041-000277|3X-26|ACTIVE
A5-42|G8952-0055|3X-23|INACTIVE
A5-42A|266744|4X-07|ACTIVE
A5-53|12140018903|5X-14|ACTIVE
A6-31|MV0041-000278|3X-26|INACTIVE
A6-31a|G8952-0084|3X-23|ACTIVE
A6-32|MV0041-000277|3X-26|INACTIVE
A6-32A|G8952-0053|3X-23|ACTIVE
A6-34|G8952-0054|3X-23|ACTIVE
A6-35|G8952-0055|3X-23|ACTIVE
A6-53|12620000553|5X-12|ACTIVE
A7-31|AZMY4002|3X-29|ACTIVE
A7-32|AZMY3001|3X-29|ACTIVE
A7-53|12620000753|5X-12|ACTIVE
A8-31|AZMY9004|3X-29|ACTIVE
A8-32|AZMY7003|3X-29|ACTIVE
A8-53|12620000563|5X-12|ACTIVE
AV06|P11ZXK017|3X-09|INACTIVE
AV07|P122XK602|4X-16|INACTIVE
AV08|P124XKJ69|3X-09i|INACTIVE
AV09|P122XKH81|3X-09|INACTIVE
AV11|035269|4X-15|INACTIVE
AV12|035268|4X-15|INACTIVE
B1-32|203823|3X-07|ACTIVE
B1-33|P117XK535|3X-09|INACTIVE
B1-34|034768|3X-14|ACTIVE
B1-45|034769|4X-14|ACTIVE
B1-51|12140019183|5X-14|ACTIVE
B1-53|12140022203|5X-14|ACTIVE
B2-32|P117XK535|3X-09|INACTIVE
B2-32a|VD010060|3X-30|ACTIVE
B2-33|VD010061|3X-30|ACTIVE
B2-34|VD010067|3X-30|ACTIVE
B2-35|VD010068|3X-30|ACTIVE
B2-51|12620000953|5X-12|ACTIVE
B3-41|268110|4X-08|ACTIVE
B3-52|15475745484|5X-07|ACTIVE
BR01|CY-C107021|NA|ACTIVE
C-32|CMXV1231029|3X-32|INACTIVE
C-33|CMXV1231029|3X-32|ACTIVE
C-34|CMXV1241101|3X-32|ACTIVE
C-51|12140022153|5X-14|ACTIVE
C-52|12140022163|5X-14|ACTIVE
FC01|243039|4X-01|ACTIVE
FC02|244490|3X-11|ACTIVE
FC03|250436|5X-02|ACTIVE
FC04|250796|4X-01|ACTIVE
FC05|261526|4X-01|ACTIVE
FC06|261527|4X-01|ACTIVE
FC07|11880000913|5X-04|ACTIVE
FC08|M256363E20A|3X-18|ACTIVE
FC09|253410|5X-05|ACTIVE
FC10|265558|5X-06|ACTIVE
FC11|265559|5X-06|ACTIVE
FC12|15475739884|5X-07|ACTIVE
FC13|14585609704|3X-20|INACTIVE
FC13a|NV505230113|3X-31|ACTIVE
FC14|14585609674|3X-20|ACTIVE
FC15|269124|4X-01|ACTIVE
FC16|14585610364|3X-20|ACTIVE
FC17|15475739874|5X-07|ACTIVE
FC18|15475739904|5X-07|ACTIVE
GK01|034768|3X-14|INACTIVE
GK02|034769|4X-14|INACTIVE
GR01|227325|3X-08|ACTIVE
GR02|203823|3X-07|INACTIVE
GR03|232129|3X-08|INACTIVE
GR04|268110|4X-08|INACTIVE
H1-21|Y2-1286|2X-04|ACTIVE
H1-42|283496|4X-13|ACTIVE
H1-43|269004|4X-13|ACTIVE
H1-44|261684|4X-13|ACTIVE
H2-31|276775|3X-21|INACTIVE
H2-51|283192|5X-11|ACTIVE
H2-52|283193|5X-10|ACTIVE
H2-53|264750|5X-09|ACTIVE
H2-54|283194|5X-10|ACTIVE
H3-31|NV503170603|3X-22|INACTIVE
H3-34|276775|3X-21|ACTIVE
H3-35|NV503170512|3X-22|ACTIVE
H3-36|NV503170603|3X-22|ACTIVE
H3-51|11415589584|5X-03|ACTIVE
H3-52|281392|5X-08|ACTIVE
H3-53|268112|5X-08|ACTIVE
HW01|183367|2X-03|INACTIVE
HW02|186970|2X-03|INACTIVE
HW03|Y2-1286|2X-04|INACTIVE
HW05|219321|3X-01|INACTIVE
HW06|219320|3X-01|INACTIVE
HW07|219322|3X-01|INACTIVE
HW08|196093|3X-01|ACTIVE
HW09|220930|3X-01|INACTIVE
HW12|199831|4X-03|ACTIVE
HW14|231616|3X-10|INACTIVE
HW15|231615|3X-10|INACTIVE
HW16|218349|4X-03|ACTIVE
HW17|266744|4X-07|INACTIVE
HW18|266897|4X-07|INACTIVE
HW19|266896|4X-07|INACTIVE
HW20|NV503160118|4X-10|INACTIVE
HW21|NV503160125|4X-10|INACTIVE
HW22|NV503160116|4X-10|INACTIVE
HW23|NV503160117|4X-10|INACTIVE
HW24|G8940-0078|3X-19|INACTIVE
HW25|NV503160915|4X-10|INACTIVE
J1-21|219060|2X-02|ACTIVE
J1-22|233201|2X-02|ACTIVE
J1-23|212465|2X-03|ACTIVE
J1-24|218188|2X-02|ACTIVE
J1-25|217205|2X-01|ACTIVE
J1-26|178997|2X-01|ACTIVE
J1-27|178996|2X-01|ACTIVE
J1-31|G8940-0045|3X-19|INACTIVE
J1-32|G8940-0078|3X-19|INACTIVE
J2-21|186970|2X-03|INACTIVE
J2-22|178996|2X-01|INACTIVE
J2-31|G8940-0045|3X-19|ACTIVE
J2-32|220930|3X-01|ACTIVE
J2-34|G8940-0078|3X-19|ACTIVE
J2-35|182714|3X-02|ACTIVE
J2-53|200395|5X-01|ACTIVE
J2-56|182711|5X-01|ACTIVE
J3-21|178995|2X-01|ACTIVE
J3-22|178997|2X-01|INACTIVE
J4-41|NV504180623|4X-10|ACTIVE
J4-52|294600|5X-08|ACTIVE
J5-41|NV504180626|4X-10|ACTIVE
J5-52|294599|5X-08|ACTIVE
J6-41|NV504180628|4X-10|ACTIVE
J6-52|12710007873|5X-13|ACTIVE
J7-21|290299|2X-10|INACTIVE
J7-22|287706|2X-10|INACTIVE
J8-41|NV504180618|4X-10|ACTIVE
J8-52|311017|5X-08|ACTIVE
K1-31|035269|4X-15|INACTIVE
K1-32|035268|4X-15|INACTIVE
K1-33|232129|3X-08|INACTIVE
K1-34|231615|3X-10|ACTIVE
K1-41|266897|4X-07|ACTIVE
K1-42|266896|4X-07|ACTIVE
K10-21|NL153180814|2X-09|ACTIVE
K10-22|NL153170901|2X-09|ACTIVE
K10-23|NL153180416|2X-09|ACTIVE
K10-24|NL153170920|2X-09|ACTIVE
K2-31|P11ZXK017|3X-09|INACTIVE
K2-32|P122XK602|4X-16|INACTIVE
K2-34|P11ZXK017|3X-09|ACTIVE
K2-35|P124XKJ69|3X-09i|ACTIVE
K2-36|P122XKH81|3X-09|ACTIVE
K2-37|182714|3X-02|INACTIVE
K2-37a|P117XK535|3X-09|ACTIVE
K2-41|035268|4X-15|ACTIVE
K2-42|035269|4X-15|ACTIVE
K2-43|P122XK602|4X-16|ACTIVE
K3-31a|14585617994|3X-20|ACTIVE
K3-32|P124XKJ69|3X-09i|INACTIVE
K3-32a|14585618044|3X-20|ACTIVE
K3-33|NV505230113|3X-31|INACTIVE
K3-33a|14585609704|3X-20|ACTIVE
K3-34|14585613704|3X-20|ACTIVE
K4-31a|219321|3X-01|ACTIVE
K4-32|P122XKH81|3X-09|INACTIVE
K4-33|219322|3X-01|ACTIVE
K4-34|177480|3X-02|ACTIVE
K4-35|219320|3X-01|ACTIVE
K4-42|NV503160915|4X-10|ACTIVE
K5-21|NL153170901|2X-09|INACTIVE
K5-22|NL153180814|2X-09|INACTIVE
K5-41|NV503160117|4X-10|ACTIVE
K5-42|NV503160116|4X-10|ACTIVE
K5-43|NV503160125|4X-10|ACTIVE
K5-44|NV503160118|4X-10|ACTIVE
K6-21|NL153180416|2X-09|INACTIVE
K6-21a|NL153170210|2X-09|ACTIVE
K6-22|NL153170920|2X-09|INACTIVE
K6-22a|178997|2X-01|INACTIVE
K6-23|178996|2X-01|INACTIVE
K7-21|267916|2X-07|ACTIVE
K7-22|267917|2X-07|ACTIVE
K7-23|290299|2X-10|ACTIVE
K7-24|287706|2X-10|ACTIVE
K7-31|14585618044|3X-20|INACTIVE
K7-32|14585617994|3X-20|INACTIVE
K8-21|186970|2X-03|ACTIVE
K8-22|183367|2X-03|ACTIVE
K8-23|276379|2X-08|ACTIVE
K8-24|38510104|2X-06|ACTIVE
K9-21|NL154230105|2X-09|ACTIVE
K9-22|335828|2X-11|ACTIVE
K9-23|NL154230107|2X-09|ACTIVE
K9-24|335833|2X-11|ACTIVE
M1-21|219060|2X-02|INACTIVE
M1-22|218188|2X-02|INACTIVE
MD01|261684|4X-13|INACTIVE
MD02|269004|4X-13|INACTIVE
MD03|268112|5X-08|INACTIVE
MD04|15475745484|5X-07|INACTIVE
MD05|264750|5X-09|INACTIVE
MD06|276775|3X-21|INACTIVE
MD07|276379|2X-08|INACTIVE
MD08|NL153170210|2X-09|INACTIVE
MD09|NL153170210|2X-09|INACTIVE
MD10|NV503170512|3X-22|INACTIVE
MD11|NV503170603|3X-22|INACTIVE
MD12|281392|5X-08|INACTIVE
MD13|283193|5X-10|INACTIVE
MD14|283194|5X-10|INACTIVE
MD15|283192|5X-11|INACTIVE
S001|286303|4X-08|ACTIVE
S002|NL153170901|2X-09|INACTIVE
SM01|178996|2X-01|INACTIVE
SM02|178995|2X-01|INACTIVE
SM03|178997|2X-01|INACTIVE
SM05|182714|3X-02|INACTIVE
SM06|177480|3X-02|INACTIVE
SM09|200395|5X-01|INACTIVE
SM10|182712|5X-01|ACTIVE
SM11|182711|5X-01|INACTIVE
SM12|172570|5X-01|ACTIVE
SM13|183438|4X-02|ACTIVE
SM14|174085|4X-02|ACTIVE
SM15|38510104|2X-06|INACTIVE
SM16|11415589584|5X-03|INACTIVE
SM17|G8940-0045|3X-19|INACTIVE
SP00|212465|2X-03|INACTIVE
SP01|219060|2X-02|INACTIVE
SP02|218188|2X-02|INACTIVE
SP03|217205|2X-01|INACTIVE
SP04|220931|3X-01|ACTIVE
SP05|196094|3X-01|ACTIVE
SP06|219355|3X-01|ACTIVE
SP07|219356|3X-01|ACTIVE
SP08|198313|3X-07|ACTIVE
SP09|219339|3X-03|ACTIVE
SP11|218408|4X-01|ACTIVE
SP12|218407|4X-03|ACTIVE
SP13|218406|4X-03|ACTIVE
SP14|221071|4X-03|ACTIVE
SP15|230812|4X-03|ACTIVE
SP16|225314|3X-10|ACTIVE
SP17|233201|2X-02|INACTIVE
SP19|266898|4X-08|ACTIVE
SP20|NV701160110|4X-11|ACTIVE
SP21|NV701160210|4X-11|ACTIVE
SP22|275044|4X-08|ACTIVE
SP23|14585613704|3X-20|INACTIVE
TM01|267916|2X-07|INACTIVE
TM02|267917|2X-07|INACTIVE";
        return raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l =>
            {
                var p = l.Split('|');
                var name = p.Length > 0 ? p[0].Trim() : "";
                var serial = p.Length > 1 ? p[1].Trim() : "";
                var workcenter = p.Length > 2 ? p[2].Trim() : "";
                var active = p.Length > 3 && string.Equals(p[3].Trim(), "ACTIVE", StringComparison.OrdinalIgnoreCase);
                return (name, serial, workcenter, active);
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

    // ---- Reset from seed (used by Settings Reset buttons) ----

    public static void ResetUsers(ApplicationDbContext context)
    {
        context.Users.RemoveRange(context.Users.ToList());
        context.SaveChanges();
        foreach (var (displayName, username, password) in new[] { ("Adib Jamil", "adib.jamil", "123"), ("Bakhari Hussin", "bakhari.hussin", "123"), ("Faiq Faizul", "faiq.faizul", "123"), ("Hakim Hisham", "hakim.hisham", "123"), ("Hakim Ramaly", "hakim.ramaly", "123"), ("Ismail Jahrin", "ismail.jahrin", "123"), ("Low Boon Bao", "boon.bao", "123"), ("Nik Faiszal Abdullah", "nik.faiszal", "123"), ("Tan Chee Wei", "chee.wei", "123") })
            context.Users.Add(new User { Username = username, Password = password, DisplayName = displayName });
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
        var trios = new[] {
            ("AB01", "SAM", "Engine Casing"), ("AB02", "SAM", "Panther"), ("AB03", "SAM", "UTAS A350 Component"),
            ("AD01", "Meggitt Coventry", "MCS-C"), ("AD02", "Meggitt Sensing System", "MSS-F"), ("AD03", "Meggitt Akrons Braking Systems", "MABS A"),
            ("AE01", "Honeywell", "Honeywell"), ("AE02", "Honeywell", "Celestica Localisation"), ("AE03", "Honeywell", "DU1080"), ("AE04", "Plexus", "Plexus"), ("AE05", "Spirit Aerosystems", "VMI PULL"),
            ("AG01", "Spirit Aerosystems", "A350 XWB"), ("AG02", "Spirit Aerosystems", "A320 FTB-6B"), ("AG03", "Spirit Aerosystems", "Spirit Subang A320 6C"), ("AG04", "Spirit Aerosystems", "SPIRIT EPIPHRON TTI & CR"),
            ("AG05", "Spirit Aerosystems", "Spirit Wave 4"), ("AG06", "Spirit Aerosystems", "WAVE 5"), ("AG07", "Spirit Aerosystems", "A321 XLR"), ("AG08", "Spirit Aerosystems", "SPIRIT GOLD"), ("AG09", "Spirit Aerosystems", "SINARAN A320 CA"),
            ("AH01", "UTAS India", "Goodrich"), ("AH02", "UTAS / CTRM", "787 Fan Cowl"), ("AH03", "UTAS / CTRM", "A350 Fan Cowl"), ("AH04", "UTAS US", "Motor Controller Plates"), ("AH05", "UTAS US", "C-Series & MRJ Fan Cowl"),
            ("AJ01", "Celestica", "Celestica HS"),
            ("AL01", "SOGERMA, FRENCH", "A350 SOGERMA"), ("AL02", "SOGERMA, FRENCH", "A321 S14A SOGERMA"), ("AL03", "SOGERMA, FRENCH", "Celeste Seat"), ("AL04", "SOGERMA, FRENCH", "A350 MLGB"), ("AL05", "SOGERMA, FRENCH", "Celeste Seat 2nd Package"),
            ("AL06", "Stelia Aerospace", "AIRBUS D2P SBMSA"), ("AL07", "Stelia Aerospace", "STELIA D2P SBMSA"), ("AL08", "Senior Aerospace Thailand", "SAT D2P"), ("AL09", "Senior Aerospace Thailand", "SAT D2P"), ("AL10", "Airbus Atlantic", "Airbus Atlantic NPI & SB76"),
            ("AL11", "Airbus Germany", "AIRBUS GERMANY SA-A350"), ("AL12", "Airbus Germany", "AIRBUS GERMANY SA-A350-KPDD-VAR"),
            ("AM01", "GKN Aerospace", "MC130 & MC133"), ("AM03", "GKN Aerospace", "GKN A350 TE"), ("AM04", "GKN Aerospace", "GKN A330 BEARING ASSY"),
            ("AOG", "Airbus", "Airbus"), ("AP02", "Senior Aerospace Thailand", "FHI-SAT Fitting"), ("AQ01", "CTRM Aerosystems", "A350 XWB"),
            ("SA01", "Senior SSP", "SSP A320 Neo & C-Series Flanges"), ("SB01", "Senior Ermeto", "Senior Ermeto")
        };
        foreach (var (code, customer, project) in trios)
            context.ProjectCodes.Add(new ProjectCode { Code = code, Description = customer, Project = project, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        context.SaveChanges();
    }

    public static void ResetMachineNames(ApplicationDbContext context)
    {
        if (context.MachineNames == null) return;
        context.MachineNames.RemoveRange(context.MachineNames.ToList());
        context.SaveChanges();
        foreach (var (name, serial, workcenter, isActive) in GetMachineNameSeedData())
        {
            context.MachineNames.Add(new MachineName { Name = name, Description = serial, Workcenter = workcenter ?? "", CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = isActive });
        }
        context.SaveChanges();
    }

    public static void ResetMachineWorkcenters(ApplicationDbContext context)
    {
        if (context.MachineWorkcenters == null) return;
        context.MachineWorkcenters.RemoveRange(context.MachineWorkcenters.ToList());
        context.SaveChanges();
        var workcenters = "2X-01|2X-02|2X-03|2X-04|2X-06|2X-07|2X-08|2X-09|2X-10|2X-11|3X-01|3X-02|3X-03|3X-07|3X-08|3X-09|3X-09i|3X-10|3X-11|3X-14|3X-18|3X-19|3X-20|3X-21|3X-22|3X-23|3X-26|3X-27|3X-28|3X-29|3X-30|3X-31|3X-32|4X-01|4X-02|4X-03|4X-07|4X-08|4X-10|4X-11|4X-13|4X-14|4X-15|4X-16|5X-01|5X-02|5X-03|5X-04|5X-05|5X-06|5X-07|5X-08|5X-09|5X-10|5X-11|5X-12|5X-13|5X-14|5X-15|NA".Split('|');
        foreach (var wc in workcenters)
        {
            var axis = wc == "NA" ? "N/A" : wc.StartsWith("2X") ? "2-Axis" : wc.StartsWith("3X") ? "3-Axis" : wc.StartsWith("4X") ? "4-Axis" : wc.StartsWith("5X") ? "5-Axis" : "3-Axis";
            context.MachineWorkcenters.Add(new MachineWorkcenter { Workcenter = wc, Description = axis, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        }
        context.SaveChanges();
    }

    public static void ResetMachineModels(ApplicationDbContext context)
    {
        if (context.MachineModels == null) return;
        context.MachineModels.RemoveRange(context.MachineModels.ToList());
        context.SaveChanges();
        var seed = new[] {
            ("AERO-426", "Hartford", "Heidenhain", "Mill"), ("CMX1100V", "DMG", "Fanuc", "Mill"), ("DMC1150V", "DMG", "Heidenhain", "Mill"), ("DMC60H", "DMG", "Heidenhain", "Mill"),
            ("DMC80 U duoBLOCK", "DMG", "Heidenhain", "Mill"), ("DMU50", "DMG", "Heidenhain", "Mill"), ("DMU60 Evo", "DMG", "Heidenhain", "Mill"), ("DMU65 monoBLOCK", "DMG", "Heidenhain", "Mill"),
            ("DMU95 monoBLOCK", "DMG", "Heidenhain", "Mill"), ("DNM500HS", "Doosan", "Fanuc", "Mill"), ("DNM750L-II", "Doosan", "Fanuc", "Mill"), ("E17040-V2", "Ares Seiki", "Siemens", "Mill"),
            ("FANUC Robodrill a-T21iFb", "Fanuc", "Fanuc", "Mill"), ("DVD5200 Dual Spindle", "FFG DMC", "Fanuc", "Mill"), ("HCN4000 II", "Mazak", "Mazatrol", "Mill"), ("HCN4000 III", "Mazak", "Mazatrol", "Mill"),
            ("HCN6000 C", "Mazak", "Mazatrol", "Mill"), ("HCN6000 II", "Mazak", "Mazatrol", "Mill"), ("HiREX-4000", "Hwacheon", "Fanuc", "Mill"), ("HTC 4000-II", "Mazak", "Mazatrol", "Mill"),
            ("Integrex i-200", "Mazak", "Mazatrol", "MillTurn"), ("Integrex i-630V", "Mazak", "Mazatrol", "MillTurn"), ("Integrex j-200", "Mazak", "Mazatrol", "MillTurn"), ("Integrex j-200s", "Mazak", "Mazatrol", "MillTurn"),
            ("MYNX 9500", "Doosan", "Heidenhain", "Mill"), ("NLX1500", "DMG", "Celos", "MillTurn"), ("NVX5060", "DMG", "Celos", "Mill"), ("NVX5060 HT", "DMG", "Celos", "Mill"),
            ("NVX5100 (3X + Indexer)", "DMG", "Celos", "Mill"), ("NVX7000 (3X + Indexer)", "DMG", "Celos", "Mill"), ("PFH4800", "Mazak", "Mazatrol", "Mill"),
            ("QT200", "Mazak", "Mazatrol", "Lathe"), ("QTC200MSY L", "Mazak", "Mazatrol", "Lathe"), ("QTE200", "Mazak", "Mazatrol", "Lathe"), ("QTN100", "Mazak", "Mazatrol", "Lathe"),
            ("QTN100-II MSY", "Mazak", "Mazatrol", "Lathe"), ("QTN150", "Mazak", "Mazatrol", "Lathe"), ("QTN200", "Mazak", "Mazatrol", "Lathe"), ("SIRIUS-650", "Hwacheon", "Fanuc", "Mill"),
            ("TMV1600A", "TongTai", "Fanuc", "Mill"), ("TMV1600A (Indexer)", "TongTai", "Fanuc", "Mill"), ("TMV510A-II (Indexer)", "TongTai", "Fanuc", "Mill"), ("TMV1500A (3X + Indexer)", "TongTai", "Fanuc", "Mill"),
            ("Tornos Delta 38-5A", "Tornos", "Fanuc", "Lathe"), ("UM Dual Spindle Machine", "UGINT", "Mitsubishi", "Mill"), ("UM500DH", "UGINT", "Mitsubishi", "Mill"), ("UM500DH (3X + Indexer)", "UGINT", "Mitsubishi", "Mill"),
            ("UM-V500", "UGINT", "Mitsubishi", "Mill"), ("VCN410A", "Mazak", "Mazatrol", "Mill"), ("VCN410A Indexer", "Mazak", "Mazatrol", "Mill"), ("VCN410A-II", "Mazak", "Mazatrol", "Mill"),
            ("VCN430A-II HS", "Mazak", "Mazatrol", "Mill"), ("VCN510C", "Mazak", "Mazatrol", "Mill"), ("VCN510C-II", "Mazak", "Mazatrol", "Mill"), ("VCN515C", "Mazak", "Mazatrol", "Mill"),
            ("VCN530C-HS (3X + Indexer)", "Mazak", "Mazatrol", "Mill"), ("VCN535", "Mazak", "Mazatrol", "Mill"), ("VCN700D (3X + Indexer)", "Mazak", "Mazatrol", "Mill"), ("VCS430A", "Mazak", "Mazatrol", "Mill"),
            ("Victor Turning", "Victor", "Fanuc", "Lathe"), ("Vortex i-630V/6", "Mazak", "Mazatrol", "Mill"), ("VRX500", "Mazak", "Mazatrol", "Mill"), ("VRX730-5X II", "Mazak", "Mazatrol", "Mill"),
            ("VRXi-500", "Mazak", "Mazatrol", "Mill"), ("VTC200C", "Mazak", "Mazatrol", "Mill"),
        };
        foreach (var (model, builder, controller, type) in seed)
            context.MachineModels.Add(new MachineModel { Model = model, Description = builder, Type = type, Controller = controller, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        context.SaveChanges();
    }

    public static void ResetCamLeaders(ApplicationDbContext context)
    {
        if (context.CamLeaders == null) return;
        context.CamLeaders.RemoveRange(context.CamLeaders.ToList());
        context.SaveChanges();
        foreach (var (name, position) in new[] { ("Venkatesan", "Director"), ("Desmond", "HOD"), ("Adib Jamil", "CAM Manager") })
            context.CamLeaders.Add(new CamLeader { Name = name, Description = position, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        context.SaveChanges();
    }

    public static void ResetCamProgrammers(ApplicationDbContext context)
    {
        if (context.CamProgrammers == null) return;
        context.CamProgrammers.RemoveRange(context.CamProgrammers.ToList());
        context.SaveChanges();
        foreach (var (name, location) in new[] { ("Adib Jamil", "Subang Plant"), ("Bakhari Hussin", "Shah Alam Plant"), ("Faiq Faizul", "Subang Plant"), ("Hakim Hisham", "Subang Plant"), ("Hakim Ramaly", "Shah Alam Plant"), ("Ismail Jahrin", "Subang Plant"), ("Low Boon Bao", "Shah Alam Plant"), ("Nik Faiszal Abdullah", "Subang Plant"), ("Tan Chee Wei", "Shah Alam Plant") })
            context.CamProgrammers.Add(new CamProgrammer { Name = name, Description = location, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        context.SaveChanges();
    }

    public static void ResetMaterialSpecs(ApplicationDbContext context)
    {
        if (context.MaterialSpecs == null) return;
        // Null FK references first to avoid constraint issues when deleting material specs
        if (context.PartNumbers != null)
        {
            foreach (var pn in context.PartNumbers.Where(p => p.MaterialSpecId != null))
                pn.MaterialSpecId = null;
            context.SaveChanges();
        }
        context.MaterialSpecs.RemoveRange(context.MaterialSpecs.ToList());
        context.SaveChanges();
        var pairs = new[] {
            ("ABP3-2101", "Aluminum Alloy 7075"), ("ABP3-2304", "Aluminum Alloy 2024"), ("ABP3-4001", "Titanium Alloy Ti-6Al-4V"), ("ABP3-4201", "Titanium Alloy Plate"),
            ("ABP3-7101", "15-5PH Stainless Steel"), ("BMS7-304", "Aluminum Alloy 7075"), ("BMS7-26", "Aluminum Alloy 2024"), ("AMS4928", "Titanium Alloy Ti-6Al-4V"),
            ("AMS5643", "Stainless Steel 321"), ("AMS5662", "Inconel 718 Nickel Alloy"), ("BMS7-304", "Aluminum Alloy 7075-T6/T73"), ("BMS7-26", "Aluminum Alloy 2024-T3/T351"),
            ("BMS7-331", "Titanium Alloy 6AL-4V"), ("BMS7-380", "Stainless Steel 15-5PH"), ("BMS7-430", "Inconel 718 Nickel Alloy"),
        };
        foreach (var (spec, material) in pairs)
            context.MaterialSpecs.Add(new MaterialSpec { Spec = spec, Material = material, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
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
        var partSeed = new[] {
            ("351-2123-13", "HINGES-LH", "A00", "A00", "V12345", "AG01", "ABP3-2101"), ("351-2123-14", "BRACKET RH", "B00", "B00", "351-2261", "AG02", "ABP3-2304"),
            ("351-2123-15", "LINK LH", "A", "A", "D123456", "AG03", "ABP3-4001"), ("351-2123-16", "HINGES-RH", "NA", "NA", "V12346", "AH01", "ABP3-7101"),
            ("351-2123-21", "BRACKET LH", "A00", "B00", "351-2262", "AL01", "BMS7-304"), ("351-2123-22", "LINK RH", "B00", "A00", "D123457", "AL02", "AMS4928"),
            ("351-2123-23", "FITTING", "A", "NA", "V12347", "AM01", "AMS5643"), ("351-2123-24", "PLATE ASSY", "NA", "A", "351-2263", "AP02", "AMS5662"),
            ("351-2123-25", "BRACKET ASSY", "A00", "A00", "D123458", "SA01", "BMS7-331"), ("351-2123-26", "HINGES-CENTER", "B00", "B00", "V12348", "SB01", "BMS7-380"),
            ("351-2123-27", "LINK ASSY", "A", "A", "351-2264", "AG04", "BMS7-430"), ("351-2123-29", "FITTING LH", "NA", "NA", "D123459", "AE01", "ABP3-2304"),
        };
        foreach (var (name, desc, partRev, drawRev, refDraw, pcCode, msSpec) in partSeed)
        {
            var pcId = projectCodes.TryGetValue(pcCode, out var pid) ? pid : (int?)null;
            var msId = matBySpec.TryGetValue(msSpec, out var mid) ? mid : (int?)null;
            context.PartNumbers.Add(new PartNumber { Name = name, Description = desc, ProjectCodeId = pcId, PartRev = partRev, DrawingRev = drawRev, MaterialSpecId = msId, RefDrawing = refDraw, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        }
        context.SaveChanges();
    }

    public static void ResetToolCodeUniques(ApplicationDbContext context)
    {
        if (context.ToolCodeUniques == null) return;
        context.ToolCodeUniques.RemoveRange(context.ToolCodeUniques.ToList());
        context.SaveChanges();
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

    public static void ResetToolLists(ApplicationDbContext context)
    {
        context.ToolListHeaders.RemoveRange(context.ToolListHeaders.ToList());
        context.SaveChanges();
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
