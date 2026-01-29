using System.Globalization;
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
        
        // Always clear and re-seed Settings tables on each run so defaults stay in sync
        context.Users.RemoveRange(context.Users.ToList());
        context.SaveChanges();
        foreach (var (displayName, username, password) in new[] { ("Adib Jamil", "adib.jamil", "123"), ("Bakhari Hussin", "bakhari.hussin", "123"), ("Faiq Faizul", "faiq.faizul", "123"), ("Hakim Hisham", "hakim.hisham", "123"), ("Hakim Ramaly", "hakim.ramaly", "123"), ("Ismail Jahrin", "ismail.jahrin", "123"), ("Low Boon Bao", "boon.bao", "123"), ("Nik Faiszal Abdullah", "nik.faiszal", "123"), ("Tan Chee Wei", "chee.wei", "123") })
        {
            context.Users.Add(new User { Username = username, Password = password, DisplayName = displayName });
        }
        context.SaveChanges();
        
        try
        {
            if (context.ProjectCodes != null)
            {
                context.ProjectCodes.RemoveRange(context.ProjectCodes.ToList());
                context.SaveChanges();
                foreach (var code in "AG01|AG02|AG03|AG04|AG07|AG08|AG09|AH02|AH03|AH05|AL01|AL02|AL04|AL06|AL07|AL08|AL09|AL10|AL11|AM01|AM03|AM04|AOG|AP02|AQ01|SA01|SB01".Split('|'))
                {
                    context.ProjectCodes.Add(new ProjectCode { Code = code, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
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
                foreach (var name in "A002|A003|A004|A006|A007|A008|A009|A010|A011|A012|A013|A014|A015|A016|A017|A018|A019|A020|A021|A022|A023|A024|A025|A026|A1-31|A1-33|A1-52|A2-31|A2-52|A2-53|A3-31|A3-52|A4-31|A4-31a|A4-32|A4-53|A5-31|A5-31A|A5-42|A5-42A|A5-53|A6-31|A6-31a|A6-32|A6-32A|A6-34|A6-35|A6-53|A7-31|A7-32|A7-53|A8-31|A8-32|A8-53|AV06|AV07|AV08|AV09|AV11|AV12|B1-32|B1-33|B1-34|B1-45|B1-51|B1-53|B2-32|B2-32a|B2-33|B2-34|B2-35|B2-51|B3-41|B3-52|BR01|C-32|C-33|C-34|C-51|C-52|FC01|FC02|FC03|FC04|FC05|FC06|FC07|FC08|FC09|FC10|FC11|FC12|FC13|FC13a|FC14|FC15|FC16|FC17|FC18|GK01|GK02|GR01|GR02|GR03|GR04|H1-21|H1-42|H1-43|H1-44|H2-31|H2-51|H2-52|H2-53|H2-54|H3-31|H3-34|H3-35|H3-36|H3-51|H3-52|H3-53|HW01|HW02|HW03|HW05|HW06|HW07|HW08|HW09|HW12|HW14|HW15|HW16|HW17|HW18|HW19|HW20|HW21|HW22|HW23|HW24|HW25|J1-21|J1-22|J1-23|J1-24|J1-25|J1-26|J1-27|J1-31|J1-32|J2-21|J2-22|J2-31|J2-32|J2-34|J2-35|J2-53|J2-56|J3-21|J3-22|J4-41|J4-52|J5-41|J5-52|J6-41|J6-52|J7-21|J7-22|J8-41|J8-52|K1-31|K1-32|K1-33|K1-34|K1-41|K1-42|K10-21|K10-22|K10-23|K10-24|K2-31|K2-32|K2-34|K2-35|K2-36|K2-37|K2-37a|K2-41|K2-42|K2-43|K3-31a|K3-32|K3-32a|K3-33|K3-33a|K3-34|K4-31a|K4-32|K4-33|K4-34|K4-35|K4-42|K5-21|K5-22|K5-41|K5-42|K5-43|K5-44|K6-21|K6-21a|K6-22|K6-22a|K6-23|K7-21|K7-22|K7-23|K7-24|K7-31|K7-32|K8-21|K8-22|K8-23|K8-24|K9-21|K9-22|K9-23|K9-24|M1-21|M1-22|MD01|MD02|MD03|MD04|MD05|MD06|MD07|MD08|MD09|MD10|MD11|MD12|MD13|MD14|MD15|S001|S002|SM01|SM02|SM03|SM05|SM06|SM09|SM10|SM11|SM12|SM13|SM14|SM15|SM16|SM17|SP00|SP01|SP02|SP03|SP04|SP05|SP06|SP07|SP08|SP09|SP11|SP12|SP13|SP14|SP15|SP16|SP17|SP19|SP20|SP21|SP22|SP23|TM01|TM02".Split('|'))
                {
                    context.MachineNames.Add(new MachineName { Name = name, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MachineWorkcenters != null)
            {
                context.MachineWorkcenters.RemoveRange(context.MachineWorkcenters.ToList());
                context.SaveChanges();
                foreach (var workcenter in "2X-01|2X-02|2X-03|2X-04|2X-06|2X-07|2X-08|2X-09|2X-10|2X-11|3X-01|3X-02|3X-03|3X-07|3X-08|3X-09|3X-09i|3X-10|3X-11|3X-14|3X-18|3X-19|3X-20|3X-21|3X-22|3X-23|3X-26|3X-27|3X-28|3X-29|3X-30|3X-31|3X-32|4X-01|4X-02|4X-03|4X-07|4X-08|4X-10|4X-11|4X-13|4X-14|4X-15|4X-16|5X-01|5X-02|5X-03|5X-04|5X-05|5X-06|5X-07|5X-08|5X-09|5X-10|5X-11|5X-12|5X-13|5X-14|5X-15".Split('|'))
                {
                    context.MachineWorkcenters.Add(new MachineWorkcenter { Workcenter = workcenter, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MachineModels != null)
            {
                context.MachineModels.RemoveRange(context.MachineModels.ToList());
                context.SaveChanges();
                var models = "AERO-426|CMX1100V|DMC1150V|DMC60H|DMC80 U duoBLOCK|DMU50|DMU60 Evo|DMU65 monoBLOCK|DMU95 monoBLOCK|DNM500HS|DNM750L-II|E17040-V2|FANUC Robodrill a-T21iFb|DVD5200 Dual Spindle|HCN4000 II|HCN4000 III|HCN6000 C|HCN6000 II|HiREX-4000|HTC 4000-II|Integrex i-200|Integrex i-630V|Integrex j-200|Integrex j-200s|MYNX 9500|NLX1500|NVX5060|NVX5060 HT|NVX5100 (3X + Indexer)|NVX7000 (3X + Indexer)|PFH4800|QT200|QTC200MSY L|QTE200|QTN100|QTN100-II MSY|QTN150|QTN200|SIRIUS-650|TMV1600A|TMV1600A (Indexer)|TMV510A-II (Indexer)|TMV1500A (3X + Indexer)|Tornos Delta 38-5A|UM Dual Spindle Machine|UM500DH|UM500DH (3X + Indexer)|UM-V500|VCN410A|VCN410A Indexer|VCN410A-II|VCN430A-II HS|VCN510C|VCN510C-II|VCN515C|VCN530C-HS (3X + Indexer)|VCN535|VCN700D (3X + Indexer)|VCS430A|Victor|Vortex i-630V/6|VRX500|VRX730-5X II|VRXi-500|VTC200C".Split('|');
                foreach (var m in models)
                {
                    context.MachineModels.Add(new MachineModel { Model = m, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.CamLeaders != null)
            {
                context.CamLeaders.RemoveRange(context.CamLeaders.ToList());
                context.SaveChanges();
                foreach (var name in new[] { "Venkatesan", "Desmond", "Adib Jamil" })
                {
                    context.CamLeaders.Add(new CamLeader { Name = name, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.CamProgrammers != null)
            {
                context.CamProgrammers.RemoveRange(context.CamProgrammers.ToList());
                context.SaveChanges();
                foreach (var name in new[] { "Adib Jamil", "Bakhari Hussin", "Faiq Faizul", "Hakim Hisham", "Hakim Ramaly", "Ismail Jahrin", "Low Boon Bao", "Nik Faiszal Abdullah", "Tan Chee Wei" })
                {
                    context.CamProgrammers.Add(new CamProgrammer { Name = name, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.PartNumbers != null)
            {
                context.PartNumbers.RemoveRange(context.PartNumbers.ToList());
                context.SaveChanges();
                foreach (var name in new[] { "351-2123-13", "351-2123-14", "351-2123-15", "351-2123-16", "351-2123-21", "351-2123-22", "351-2123-23", "351-2123-24", "351-2123-25", "351-2123-26", "351-2123-27", "351-2123-29" })
                {
                    context.PartNumbers.Add(new PartNumber { Name = name, Description = null, CreatedDate = DateTime.UtcNow, CreatedBy = "system", IsActive = true });
                }
                context.SaveChanges();
            }
        }
        catch { }
        
        try
        {
            if (context.MaterialSpecs != null)
            {
                context.MaterialSpecs.RemoveRange(context.MaterialSpecs.ToList());
                context.SaveChanges();
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
