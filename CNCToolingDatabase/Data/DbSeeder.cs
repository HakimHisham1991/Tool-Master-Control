using CNCToolingDatabase.Models;

namespace CNCToolingDatabase.Data;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User { Username = "user", Password = "123", DisplayName = "User" },
                new User { Username = "john", Password = "123", DisplayName = "John Smith" },
                new User { Username = "kim", Password = "123", DisplayName = "Kim Lee" }
            );
            context.SaveChanges();
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
            
            foreach (var header in toolLists)
            {
                foreach (var detail in header.Details)
                {
                    UpdateToolMaster(context, detail);
                }
            }
            context.SaveChanges();
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
                ArborCode = tool.arbor
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
