using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using CNCToolingDatabase.Models;
using Microsoft.EntityFrameworkCore;

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
                foreach (var (name, desc, partRev, drawRev, pcCode, refDrawing, msSpec) in GetPartNumberSeedData())
                {
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
            var masterLookup = GetToolCodeUniqueSeedData()
                .ToDictionary(t => t.Consumable, t => (t.SystemToolName, t.Supplier, t.Dia, t.Flute, t.Radius));
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
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var (systemName, consumable, supplier, dia, flute, radius) in GetToolCodeUniqueSeedData())
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

    /// <summary>Machine Name seed from MACHINE NAME MASTER.txt. Format: Machine Name|Serial Number|Machine Workcenter|Machine Model|Status.</summary>
    private static (string Name, string Serial, string Workcenter, string MachineModel, bool IsActive)[] GetMachineNameSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "MACHINE NAME MASTER.txt");
        if (!File.Exists(path))
            return Array.Empty<(string, string, string, string, bool)>();
        var lines = File.ReadAllLines(path);
        var result = new List<(string, string, string, string, bool)>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split('|');
            if (cols.Length < 5 || string.IsNullOrWhiteSpace(cols[0])) continue;
            var name = cols[0].Trim();
            var serial = cols.Length > 1 ? cols[1].Trim() : "";
            var workcenter = cols.Length > 2 ? cols[2].Trim() : "";
            var machineModel = cols.Length > 3 ? cols[3].Trim() : "";
            var isActive = cols.Length > 4 && string.Equals(cols[4].Trim(), "ACTIVE", StringComparison.OrdinalIgnoreCase);
            result.Add((name, serial, workcenter, machineModel, isActive));
        }
        return result.ToArray();
    }

    /// <summary>Hard-coded Master Tool Code seed. Same data every reset, like Settings pages.</summary>
    private static List<(string SystemToolName, string Consumable, string Supplier, decimal Dia, decimal Flute, decimal Radius)> GetToolCodeUniqueSeedData()
    {
        return new List<(string, string, string, decimal, decimal, decimal)>
        {
            ("Endmill Ø12 R0.0 x 3-Flute", "553120Z3.0-SIRON-A", "SECO", 12.0m, 28.0m, 0m),
            ("Endmill Ø16 R0.5 x 3-Flute", "553160R050Z3.0-SIRON-A", "SECO", 16.0m, 32.0m, 0.5m),
            ("Endmill Ø8 R0.0 x 3-Flute", "553080Z3.0-SIRON-A", "SECO", 8.0m, 22.0m, 0m),
            ("Endmill Ø10 R0.25 x 3-Flute", "553100R250Z3.0-SIRON-A", "SECO", 10.0m, 26.0m, 0.25m),
            ("Endmill Ø10 R0.0 x 3-Flute", "553100Z3.0-SIRON-A", "SECO", 10.0m, 26.0m, 0m),
            ("Endmill Ø9.8 R0.0 x 3-Flute", "A3389DPL-9.8", "WALTER", 9.8m, 24.0m, 0m),
            ("Endmill Ø12 R0.0 x 3-Flute", "A3389DPL-12", "WALTER", 12.0m, 30.0m, 0m),
            ("Endmill Ø2.55 R0.0 x 3-Flute", "A3389AML-2.55", "WALTER", 2.55m, 8.0m, 0m),
            ("Endmill Ø6.1 R0.0 x 3-Flute", "A3389DPL-6.1", "WALTER", 6.1m, 18.0m, 0m),
            ("Endmill Ø8.5 R0.0 x 3-Flute", "A3389DPL-8.5", "WALTER", 8.5m, 22.0m, 0m),
            ("Drill Ø16 x 140°", "7792VXP06CA016Z2R140", "KENNAMETAL", 16.0m, 45.0m, 1.4m),
            ("Facemill Ø32 R3.0 x 6-Flute", "7792VXD09WA032Z3R", "KENNAMETAL", 32.0m, 55.0m, 3.0m),
            ("Facemill Ø52 R5.0 x 6-Flute", "7792VXD12-A052Z5R", "KENNAMETAL", 52.0m, 65.0m, 5.0m),
            ("Facemill Ø80 R8.0 x 6-Flute", "7792VXD12-A080Z8R", "KENNAMETAL", 80.0m, 85.0m, 8.0m),
            ("Endmill Ø4 R0.0 x 3-Flute", "553040Z3.0-SIRON-A", "SECO", 4.0m, 14.0m, 0m),
            ("Endmill Ø6 R0.0 x 3-Flute", "553060Z3.0-SIRON-A", "SECO", 6.0m, 18.0m, 0m),
            ("Endmill Ø14 R0.0 x 3-Flute", "553140Z3.0-SIRON-A", "SECO", 14.0m, 32.0m, 0m),
            ("Endmill Ø18 R1.0 x 3-Flute", "553180R100Z3.0-SIRON-A", "SECO", 18.0m, 38.0m, 1.0m),
            ("Endmill Ø20 R0.0 x 3-Flute", "553200Z3.0-SIRON-A", "SECO", 20.0m, 42.0m, 0m),
            ("Endmill Ø25 R0.0 x 3-Flute", "553250Z3.0-SIRON-A", "SECO", 25.0m, 50.0m, 0m),
            ("Endmill Ø12 R0.25 x 3-Flute", "553120R025Z3.0-SIRON-A", "SECO", 12.0m, 28.0m, 0.25m),
            ("Endmill Ø14 R0.5 x 3-Flute", "553140R050Z3.0-SIRON-A", "SECO", 14.0m, 32.0m, 0.5m),
            ("Endmill Ø16 R0.0 x 3-Flute", "553160Z3.0-SIRON-A", "SECO", 16.0m, 36.0m, 0m),
            ("Endmill Ø10 R0.2 x 3-Flute", "553100R200Z3.0-SIRON-A", "SECO", 10.0m, 26.0m, 0.2m),
            ("Endmill Ø8 R0.5 x 3-Flute", "553080R050Z3.0-SIRON-A", "SECO", 8.0m, 22.0m, 0.5m),
            ("Endmill Ø22 R0.0 x 3-Flute", "553220Z3.0-SIRON-A", "SECO", 22.0m, 46.0m, 0m),
            ("Endmill Ø30 R0.0 x 3-Flute", "553300Z3.0-SIRON-A", "SECO", 30.0m, 58.0m, 0m),
            ("Endmill Ø16 R1.0 x 3-Flute", "553160R100Z3.0-SIRON-A", "SECO", 16.0m, 36.0m, 1.0m),
            ("Endmill Ø6 R0.25 x 3-Flute", "553060R025Z3.0-SIRON-A", "SECO", 6.0m, 18.0m, 0.25m),
            ("Endmill Ø14 R0.25 x 3-Flute", "553140R025Z3.0-SIRON-A", "SECO", 14.0m, 32.0m, 0.25m),
            ("Endmill Ø18 R0.0 x 3-Flute", "553180Z3.0-SIRON-A", "SECO", 18.0m, 38.0m, 0m),
            ("Endmill Ø10 R0.0 x 3-Flute", "A3389DPL-10", "WALTER", 10.0m, 26.0m, 0m),
            ("Endmill Ø11 R0.0 x 3-Flute", "A3389DPL-11", "WALTER", 11.0m, 28.0m, 0m),
            ("Endmill Ø3 R0.0 x 3-Flute", "A3389AML-3.0", "WALTER", 3.0m, 10.0m, 0m),
            ("Endmill Ø4 R0.0 x 3-Flute", "A3389AML-4.0", "WALTER", 4.0m, 12.0m, 0m),
            ("Endmill Ø7 R0.0 x 3-Flute", "A3389DPL-7.0", "WALTER", 7.0m, 20.0m, 0m),
            ("Endmill Ø2 R0.0 x 3-Flute", "A3389AML-2.0", "WALTER", 2.0m, 6.0m, 0m),
            ("Endmill Ø5 R0.0 x 3-Flute", "A3389DPL-5.0", "WALTER", 5.0m, 16.0m, 0m),
            ("Endmill Ø14 R0.0 x 3-Flute", "A3389DPL-14", "WALTER", 14.0m, 34.0m, 0m),
            ("Endmill Ø5 R0.0 x 3-Flute", "A3389AML-5.0", "WALTER", 5.0m, 14.0m, 0m),
            ("Endmill Ø15 R0.0 x 3-Flute", "A3389DPL-15", "WALTER", 15.0m, 36.0m, 0m),
            ("Facemill Ø40 R4.0 x 6-Flute", "7792VXD10-A040Z4R", "KENNAMETAL", 40.0m, 60.0m, 4.0m),
            ("Facemill Ø100 R10.0 x 6-Flute", "7792VXD16-A100Z10R", "KENNAMETAL", 100.0m, 110.0m, 10.0m),
            ("Drill Ø20 x 160°", "7792VXP08CA020Z2R160", "KENNAMETAL", 20.0m, 55.0m, 1.6m),
            ("Facemill Ø63 R6.0 x 6-Flute", "7792VXD12-A063Z6R", "KENNAMETAL", 63.0m, 75.0m, 6.0m),
            ("Facemill Ø50 R5.0 x 6-Flute", "7792VXD12-A050Z5R", "KENNAMETAL", 50.0m, 62.0m, 5.0m),
            ("Facemill Ø70 R7.0 x 6-Flute", "7792VXD14-A070Z7R", "KENNAMETAL", 70.0m, 82.0m, 7.0m),
            ("Facemill Ø32 R3.0 x 6-Flute", "7792VXD08-A032Z3R", "KENNAMETAL", 32.0m, 48.0m, 3.0m),
            ("Drill Ø24 x 180°", "7792VXP10CA024Z2R180", "KENNAMETAL", 24.0m, 60.0m, 1.8m),
            ("Facemill Ø120 R12.0 x 6-Flute", "7792VXD20-A120Z12R", "KENNAMETAL", 120.0m, 130.0m, 12.0m),
        };
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

    /// <summary>Part Number seed from PART NUMBERS MASTER.txt. Format: Part Number|Description|Part Revision|Drawing Revision|Project Code|Ref. Drawing|Material Spec.|Material</summary>
    private static (string Name, string Description, string PartRev, string DrawingRev, string ProjectCode, string RefDrawing, string MaterialSpec)[] GetPartNumberSeedData()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "PART NUMBERS MASTER.txt");
        if (!File.Exists(path))
            return Array.Empty<(string, string, string, string, string, string, string)>();
        var lines = File.ReadAllLines(path);
        var result = new List<(string, string, string, string, string, string, string)>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split('|');
            // Support both formats: with Ref. Drawing (8 cols) or legacy (6 cols: no Ref. Drawing, Material Spec. at 5)
            if (cols.Length < 6 || string.IsNullOrWhiteSpace(cols[0])) continue;
            var name = cols[0].Trim();
            var desc = cols.Length > 1 ? cols[1].Trim() : "";
            var partRev = cols.Length > 2 ? cols[2].Trim() : "";
            var drawRev = cols.Length > 3 ? cols[3].Trim() : "";
            var pcCode = cols.Length > 4 ? cols[4].Trim() : "";
            string refDrawing;
            string msSpec;
            if (cols.Length >= 7)
            {
                refDrawing = cols.Length > 5 ? cols[5].Trim() : "";
                msSpec = cols.Length > 6 ? cols[6].Trim() : "";
            }
            else
            {
                refDrawing = "";
                msSpec = cols.Length > 5 ? cols[5].Trim() : "";
            }
            result.Add((name, desc, partRev, drawRev, pcCode, refDrawing, msSpec));
        }
        return result.ToArray();
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
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, desc, partRev, drawRev, pcCode, refDrawing, msSpec) in GetPartNumberSeedData())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!seenNames.Add(name)) continue; // skip duplicate part number names (keep first)
            var pcId = !string.IsNullOrEmpty(pcCode) && projectCodes.TryGetValue(pcCode, out var pid) ? pid : (int?)null;
            var msId = !string.IsNullOrEmpty(msSpec) && matBySpec.TryGetValue(msSpec, out var mid) ? mid : (int?)null;
            context.PartNumbers.Add(new PartNumber { Name = name, Description = desc, ProjectCodeId = pcId, PartRev = partRev, DrawingRev = drawRev, MaterialSpecId = msId, RefDrawing = refDrawing ?? "", CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
        }
        context.SaveChanges();
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
            foreach (var (systemName, consumable, supplier, dia, flute, radius) in GetToolCodeUniqueSeedData())
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
        var masterLookup = GetToolCodeUniqueSeedData()
            .ToDictionary(t => t.Consumable, t => (t.SystemToolName, t.Supplier, t.Dia, t.Flute, t.Radius));
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
