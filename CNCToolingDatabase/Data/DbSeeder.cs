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
                    
                    CREATE INDEX IF NOT EXISTS IX_ProjectCodes_Code ON ProjectCodes(Code);
                    CREATE INDEX IF NOT EXISTS IX_MachineNames_Name ON MachineNames(Name);
                    CREATE INDEX IF NOT EXISTS IX_MachineWorkcenters_Workcenter ON MachineWorkcenters(Workcenter);
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
        
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User { Username = "user", Password = "123", DisplayName = "User" },
                new User { Username = "john", Password = "123", DisplayName = "John Smith" },
                new User { Username = "kim", Password = "123", DisplayName = "Kim Lee" }
            );
            context.SaveChanges();
        }
        
        // Seed Project Codes
        try
        {
            if (context.ProjectCodes != null && !context.ProjectCodes.Any())
            {
            var projectCodes = new[]
            {
                "AG01", "AG02", "AG03", "AG07", "AG09",
                "AH03", "AH05", "AL02", "AL07", "AM03"
            };
            
            foreach (var code in projectCodes)
            {
                context.ProjectCodes.Add(new ProjectCode
                {
                    Code = code,
                    Description = null,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "system",
                    IsActive = true
                });
            }
            context.SaveChanges();
            }
        }
        catch
        {
            // Table might not exist yet, skip seeding
        }
        
        // Seed Machine Names
        try
        {
            if (context.MachineNames != null && !context.MachineNames.Any())
            {
            var machineNames = new[]
            {
                "J1-25", "SP11", "S001", "SP19", "K5-42",
                "K5-43", "SP20", "SP21", "H1-42", "FC11",
                "FC12", "FC17", "FC18", "H3-52", "A8-32",
                "A2-52", "A3-52", "A4-53", "A5-53", "B1-51"
            };
            
            foreach (var name in machineNames)
            {
                context.MachineNames.Add(new MachineName
                {
                    Name = name,
                    Description = null,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "system",
                    IsActive = true
                });
            }
            context.SaveChanges();
            }
        }
        catch
        {
            // Table might not exist yet, skip seeding
        }
        
        // Seed Machine Workcenters
        try
        {
            if (context.MachineWorkcenters != null && !context.MachineWorkcenters.Any())
            {
            var workcenters = new[]
            {
                "2X-01", "2X-02", "2X-03", "3X-03", "3X-07",
                "5X-01", "5X-02", "5X-03", "5X-04", "5X-05",
                "5X-06", "5X-07", "5X-08", "3X-26", "3X-27",
                "3X-28", "3X-29", "3X-30", "5X-14", "5X-15"
            };
            
            foreach (var workcenter in workcenters)
            {
                context.MachineWorkcenters.Add(new MachineWorkcenter
                {
                    Workcenter = workcenter,
                    Description = null,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "system",
                    IsActive = true
                });
            }
            context.SaveChanges();
            }
        }
        catch
        {
            // Table might not exist yet, skip seeding
        }
        
        if (!context.ToolListHeaders.Any())
        {
            var toolLists = new List<ToolListHeader>
            {
                CreateToolListWithDetails("PART-001", "OP10", "REV00", "PRJ-2024-001", "DMG-MORI-NHX4000", "WC-001", "john"),
                CreateToolListWithDetails("PART-001", "OP20", "REV00", "PRJ-2024-001", "DMG-MORI-NHX4000", "WC-001", "john"),
                CreateToolListWithDetails("PART-002", "OP10", "REV00", "PRJ-2024-002", "MAZAK-INTEGREX", "WC-002", "kim"),
                CreateToolListWithDetails("PART-003", "OP10", "REV00", "PRJ-2024-003", "HAAS-VF2", "WC-003", "user"),
                CreateToolListWithDetails("PART-003", "OP20", "REV00", "PRJ-2024-003", "HAAS-VF2", "WC-003", "user"),
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
    }
    
    private static ToolListHeader CreateToolListWithDetails(string partNumber, string operation, string revision, 
        string projectCode, string machineName, string workcenter, string createdBy)
    {
        var header = new ToolListHeader
        {
            PartNumber = partNumber,
            Operation = operation,
            Revision = revision,
            ProjectCode = projectCode,
            MachineName = machineName,
            MachineWorkcenter = workcenter,
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
