using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Models.ViewModels;
using CNCToolingDatabase.Services;
using CNCToolingDatabase.Data;
using System.Text;
using CNCToolingDatabase.Helpers;
using ClosedXML.Excel;

namespace CNCToolingDatabase.Controllers;

public class ToolListEditorController : Controller
{
    private readonly IToolListService _toolListService;
    private readonly ApplicationDbContext _context;
    private readonly PdfLayoutService _pdfLayoutService;
    
    public ToolListEditorController(IToolListService toolListService, ApplicationDbContext context, PdfLayoutService pdfLayoutService)
    {
        _toolListService = toolListService;
        _context = context;
        _pdfLayoutService = pdfLayoutService;
    }
    
    public async Task<IActionResult> Index(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            var viewModel = await _toolListService.GetToolListForEditAsync(id.Value, username);
            return View(viewModel);
        }
        
        var newViewModel = await _toolListService.CreateNewToolListAsync();
        return View(newViewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveToolListRequest request)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var result = await _toolListService.SaveToolListAsync(request, username);
        
        return Json(new { success = result.Success, message = result.Message, id = result.Id });
    }
    
    [HttpPost]
    public async Task<IActionResult> Close(int? id)
    {
        // Accept id from query string, route parameter, or form data (for sendBeacon)
        var toolListId = id ?? 0;
        if (toolListId == 0)
        {
            // Try query string first
            if (Request.Query.ContainsKey("id"))
            {
                int.TryParse(Request.Query["id"].ToString(), out toolListId);
            }
            // Then try form data (for sendBeacon with URLSearchParams)
            else if (Request.Form.ContainsKey("id"))
            {
                int.TryParse(Request.Form["id"].ToString(), out toolListId);
            }
        }
        
        if (toolListId > 0)
        {
            var username = HttpContext.Session.GetString("Username") ?? "";
            await _toolListService.ReleaseToolListLockAsync(toolListId, username);
        }
        
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> Heartbeat(int id)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        await _toolListService.UpdateHeartbeatAsync(id, username);
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var displayName = HttpContext.Session.GetString("DisplayName") ?? HttpContext.Session.GetString("Username") ?? "";
        if (!userId.HasValue)
            return Json(new { success = false, message = "You must be logged in to approve." });
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header == null)
            return Json(new { success = false, message = "Tool list not found." });
        if (header.ApprovedByUserId.HasValue)
            return Json(new { success = false, message = "Stamp 1 is already approved." });
        header.ApprovedByUserId = userId.Value;
        header.ApprovedBy = displayName;
        header.ApprovedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var approvedDateFormatted = header.ApprovedDate?.ToString("dd/MM/yyyy") ?? "";
        return Json(new { success = true, approvedByUserId = userId.Value, approvedDateFormatted, approvedByName = displayName });
    }
    
    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header == null)
            return Json(new { success = false, message = "Tool list not found." });
        if (!header.ApprovedByUserId.HasValue)
            return Json(new { success = false, message = "No stamp to reject." });
        if (header.CamLeaderApprovedByUserId.HasValue)
            return Json(new { success = false, message = "Reject Stamp 2 first." });
        header.ApprovedByUserId = null;
        header.ApprovedBy = "";
        header.ApprovedDate = null;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> ApproveCamLeader(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return Json(new { success = false, message = "You must be logged in to approve." });
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header == null)
            return Json(new { success = false, message = "Tool list not found." });
        if (!header.ApprovedByUserId.HasValue)
            return Json(new { success = false, message = "Complete Stamp 1 first." });
        if (header.CamLeaderApprovedByUserId.HasValue)
            return Json(new { success = false, message = "Stamp 2 is already approved." });
        header.CamLeaderApprovedByUserId = userId.Value;
        header.CamLeaderApprovedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var approvedDateFormatted = header.CamLeaderApprovedDate?.ToString("dd/MM/yyyy") ?? "";
        return Json(new { success = true, approvedByUserId = userId.Value, approvedDateFormatted });
    }
    
    [HttpPost]
    public async Task<IActionResult> RejectCamLeader(int id)
    {
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header == null)
            return Json(new { success = false, message = "Tool list not found." });
        if (!header.CamLeaderApprovedByUserId.HasValue)
            return Json(new { success = false, message = "No stamp to reject." });
        if (header.ToolRegisterByUserId.HasValue)
            return Json(new { success = false, message = "Reject Stamp 3 first." });
        header.CamLeaderApprovedByUserId = null;
        header.CamLeaderApprovedDate = null;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> ApproveToolRegister(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var displayName = HttpContext.Session.GetString("DisplayName") ?? HttpContext.Session.GetString("Username") ?? "";
        if (!userId.HasValue)
            return Json(new { success = false, message = "You must be logged in to approve." });
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header == null)
            return Json(new { success = false, message = "Tool list not found." });
        if (!header.CamLeaderApprovedByUserId.HasValue)
            return Json(new { success = false, message = "Complete Stamp 2 first." });
        if (header.ToolRegisterByUserId.HasValue)
            return Json(new { success = false, message = "Stamp 3 is already approved." });
        header.ToolRegisterByUserId = userId.Value;
        header.ToolRegisterByDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var approvedDateFormatted = header.ToolRegisterByDate?.ToString("dd/MM/yyyy") ?? "";
        return Json(new { success = true, approvedByUserId = userId.Value, approvedDateFormatted, approvedByName = displayName });
    }
    
    [HttpPost]
    public async Task<IActionResult> RejectToolRegister(int id)
    {
        var header = await _context.ToolListHeaders.FindAsync(id);
        if (header == null)
            return Json(new { success = false, message = "Tool list not found." });
        if (!header.ToolRegisterByUserId.HasValue)
            return Json(new { success = false, message = "No stamp to reject." });
        header.ToolRegisterByUserId = null;
        header.ToolRegisterByDate = null;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAvailableToolLists(string? search)
    {
        var toolLists = await _toolListService.GetAvailableToolListsAsync(search);
        return Json(toolLists);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPartNumbers()
    {
        var partNumberSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var partNumberToProjectCode = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        
        // From PartNumbers settings table (if it exists and has data) - include Project Code from Part Number Management
        var partNumberToDescription = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var fromTable = await _context.PartNumbers
                .Include(p => p.ProjectCode)
                .Select(p => new { p.Name, p.Description, ProjectCode = p.ProjectCode != null ? p.ProjectCode.Code : (string?)null })
                .ToListAsync();
            foreach (var item in fromTable)
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    partNumberSet.Add(item.Name);
                    partNumberToProjectCode[item.Name] = item.ProjectCode;
                    partNumberToDescription[item.Name] = item.Description;
                }
            }
        }
        catch
        {
            // PartNumbers table may not exist, continue
        }
        
        // Also include distinct part numbers from existing tool list headers
        var fromHeaders = await _context.ToolListHeaders
            .Select(h => h.PartNumber)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToListAsync();
        foreach (var name in fromHeaders)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                partNumberSet.Add(name);
                if (!partNumberToProjectCode.ContainsKey(name))
                    partNumberToProjectCode[name] = null;
                if (!partNumberToDescription.ContainsKey(name))
                    partNumberToDescription[name] = null;
            }
        }
        
        var result = partNumberSet
            .OrderBy(s => s)
            .Select(name => new
            {
                value = name,
                text = name,
                projectCode = partNumberToProjectCode.TryGetValue(name, out var pc) ? pc : (string?)null,
                description = partNumberToDescription.TryGetValue(name, out var desc) ? desc : (string?)null
            })
            .ToList();
        
        return Json(result);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProjectCodes()
    {
        // Include INACTIVE project codes so Project Code auto-updates when Part Number belongs to an inactive code (e.g. AJ01)
        var codes = await _context.ProjectCodes
            .OrderBy(p => p.Code)
            .Select(p => new { value = p.Code, text = p.Code })
            .ToListAsync();
        return Json(codes);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMachineNames()
    {
        var names = await _context.MachineNames
            .Include(m => m.MachineModel)
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                value = m.Name,
                text = m.Name,
                workcenter = m.Workcenter ?? "",
                machineModel = m.MachineModel != null ? m.MachineModel.Model : (string?)null
            })
            .ToListAsync();
        return Json(names);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMachineWorkcenters()
    {
        var workcenters = await _context.MachineWorkcenters
            .OrderBy(w => w.Workcenter)
            .Select(w => new { value = w.Workcenter, text = w.Workcenter })
            .ToListAsync();
        return Json(workcenters);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMachineModels()
    {
        var models = await _context.MachineModels
            .OrderBy(m => m.Model)
            .Select(m => new { value = m.Model, text = m.Model })
            .ToListAsync();
        return Json(models);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCamLeaders()
    {
        var leaders = await _context.CamLeaders
            .OrderBy(c => c.Name)
            .Select(c => new { value = c.Name, text = c.Name })
            .ToListAsync();
        return Json(leaders);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCamProgrammers()
    {
        var programmers = await _context.CamProgrammers
            .OrderBy(c => c.Name)
            .Select(c => new { value = c.Name, text = c.Name })
            .ToListAsync();
        return Json(programmers);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetOperations()
    {
        var operations = await _context.Operations
            .OrderBy(o => o.Name)
            .Select(o => new { value = o.Name, text = o.Name })
            .ToListAsync();
        return Json(operations);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetRevisions()
    {
        var revisions = await _context.Revisions
            .OrderBy(r => r.Name)
            .Select(r => new { value = r.Name, text = r.Name })
            .ToListAsync();
        return Json(revisions);
    }
    
    /// <summary>Material Specification (On Drawing) Management table. Used for dropdown in Create/Edit Tool List; General Name is auto-populated from selection.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMaterialSpecs()
    {
        var list = await _context.MaterialSpecs
            .OrderBy(m => m.Spec)
            .ThenBy(m => m.Material)
            .Select(m => new { id = m.Id, spec = m.Spec, material = m.Material })
            .ToListAsync();
        return Json(list);
    }
    
    /// <summary>Consumable Tool Descriptions from Master Tool Code Database only. Used for dropdown in Create/Edit Tool List.</summary>
    [HttpGet]
    public async Task<IActionResult> GetConsumableToolDescriptions()
    {
        var rows = await _context.ToolCodeUniques
            .AsNoTracking()
            .OrderBy(t => t.ConsumableCode)
            .Select(t => new
            {
                value = t.ConsumableCode,
                text = t.ConsumableCode,
                supplier = t.Supplier,
                diameter = t.Diameter,
                fluteLength = t.FluteLength,
                cornerRadius = t.CornerRadius
            })
            .ToListAsync();
        var distinct = rows
            .GroupBy(x => x.value)
            .Select(g =>
            {
                var f = g.First();
                return new { value = g.Key, text = g.Key, supplier = f.supplier, diameter = f.diameter, fluteLength = f.fluteLength, cornerRadius = f.cornerRadius };
            })
            .ToList();
        return Json(distinct);
    }

    /// <summary>Assembly Holder tool descriptions from Master Tool Code Database. Used for Tool Holder and Arbor Description dropdowns in Create/Edit Tool List.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAssemblyHolderDescriptions()
    {
        var rows = await _context.ToolCodeUniques
            .AsNoTracking()
            .Where(t => t.ItemCategory == "Assembly Holder")
            .OrderBy(t => t.ConsumableCode)
            .Select(t => new { value = t.ConsumableCode, text = t.ConsumableCode })
            .ToListAsync();
        var distinct = rows
            .GroupBy(x => x.value, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { value = g.Key, text = g.Key })
            .OrderBy(x => x.value)
            .ToList();
        return Json(distinct);
    }
    
    [HttpGet]
    public async Task<IActionResult> Export(int id, string format)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListForEditAsync(id, username);
        
        var formatLower = format.ToLower();
        
        // Handle Excel format with ClosedXML
        if (formatLower == "excel")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tool List");
            
            int row = 1;
            worksheet.Cell(row, 1).Value = "Tool List:";
            worksheet.Cell(row, 2).Value = viewModel.ToolListName;
            row++;
            worksheet.Cell(row, 1).Value = "Part Number:";
            worksheet.Cell(row, 2).Value = viewModel.PartNumber;
            row++;
            worksheet.Cell(row, 1).Value = "Part Description:";
            worksheet.Cell(row, 2).Value = viewModel.PartDescription;
            row++;
            worksheet.Cell(row, 1).Value = "Operation:";
            worksheet.Cell(row, 2).Value = viewModel.Operation;
            row++;
            worksheet.Cell(row, 1).Value = "Revision:";
            worksheet.Cell(row, 2).Value = viewModel.Revision;
            row++;
            worksheet.Cell(row, 1).Value = "Project Code:";
            worksheet.Cell(row, 2).Value = viewModel.ProjectCode;
            row++;
            worksheet.Cell(row, 1).Value = "Machine:";
            worksheet.Cell(row, 2).Value = viewModel.MachineName;
            row++;
            worksheet.Cell(row, 1).Value = "Workcenter:";
            worksheet.Cell(row, 2).Value = viewModel.MachineWorkcenter;
            row++;
            worksheet.Cell(row, 1).Value = "Machine Model:";
            worksheet.Cell(row, 2).Value = viewModel.MachineModel;
            row += 2;
            
            var headers = new[]
            {
                "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier",
                "Tool Holder", "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)",
                "Tool Corner Radius", "Arbor Description (or equivalent specs)",
                "Tool Path Time in Minutes", "Remarks"
            };
            
            int tableStartRow = row;
            int colCount = headers.Length;
            ExcelExportHelper.WriteHeaderRow(worksheet, row, headers);
            row++;
            
            foreach (var detail in viewModel.Details.Where(d => 
                !string.IsNullOrWhiteSpace(d.ToolNumber) || 
                !string.IsNullOrWhiteSpace(d.ConsumableCode)))
            {
                worksheet.Cell(row, 1).Value = detail.ToolNumber;
                worksheet.Cell(row, 2).Value = detail.ToolDescription;
                worksheet.Cell(row, 3).Value = detail.ConsumableCode;
                worksheet.Cell(row, 4).Value = detail.Supplier;
                worksheet.Cell(row, 5).Value = detail.HolderExtensionCode;
                worksheet.Cell(row, 6).Value = detail.Diameter ?? 0;
                worksheet.Cell(row, 7).Value = detail.FluteLength ?? 0;
                worksheet.Cell(row, 8).Value = detail.ProtrusionLength ?? 0;
                worksheet.Cell(row, 9).Value = detail.CornerRadius ?? 0;
                worksheet.Cell(row, 10).Value = detail.ArborCode;
                worksheet.Cell(row, 11).Value = detail.ToolPathTimeMinutes ?? 0;
                worksheet.Cell(row, 12).Value = detail.Remarks ?? string.Empty;
                row++;
            }
            
            ExcelExportHelper.ApplyTableBorders(worksheet, tableStartRow, row - 1, colCount);

            // 3 blank rows of space, then the approval signatures below the table.
            row += 3;
            worksheet.Cell(row, 1).Value = "CAM Programmer:";
            worksheet.Cell(row, 2).Value = viewModel.CamProgrammer;
            row++;
            worksheet.Cell(row, 1).Value = "Approved By:";
            worksheet.Cell(row, 2).Value = viewModel.ApprovedBy;

            ExcelExportHelper.AutoFitColumns(worksheet);
            
            var fileName = $"{viewModel.ToolListName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(ExcelExportHelper.SaveToBytes(workbook), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        
        // Handle PDF format
        if (formatLower == "pdf")
        {
            var details = viewModel.Details
                .Where(d => !string.IsNullOrWhiteSpace(d.ToolNumber) || !string.IsNullOrWhiteSpace(d.ConsumableCode))
                .ToList();
            // Fetch stamp images for the three approval sections.
            // The Tool Register By display name is already resolved in ToolListService.
            byte[]? stamp1 = null;
            byte[]? stamp2 = null;
            byte[]? stamp3 = null;
            if (viewModel.ApprovedByUserId.HasValue)
            {
                var u1 = await _context.Users.FindAsync(viewModel.ApprovedByUserId.Value);
                stamp1 = u1?.Stamp;
            }
            if (viewModel.CamLeaderApprovedByUserId.HasValue)
            {
                var u2 = await _context.Users.FindAsync(viewModel.CamLeaderApprovedByUserId.Value);
                stamp2 = u2?.Stamp;
            }
            if (viewModel.ToolRegisterByUserId.HasValue)
            {
                var u3 = await _context.Users.FindAsync(viewModel.ToolRegisterByUserId.Value);
                stamp3 = u3?.Stamp;
            }
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Data", "LOGO", "ZENIX.png");
            var baseDir = AppContext.BaseDirectory;
            var partImageDir = Path.Combine(baseDir, "Data", "PART_IMAGE");
            var partNumber = viewModel.PartNumber ?? "";
            var partImagePath = (string?)null;
            if (!string.IsNullOrWhiteSpace(partNumber))
            {
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".gif" })
                {
                    var p = Path.Combine(partImageDir, partNumber + ext);
                    if (System.IO.File.Exists(p)) { partImagePath = p; break; }
                }
            }
            var toolSpecsPath = Path.Combine(baseDir, "Data", "PDF_EXPORT", "TOOL_SPECS.png");
            var activeLayout = await _pdfLayoutService.GetActiveDocumentAsync();
            var pdfBytes = ToolListPdfGenerator.Generate(
                viewModel,
                details,
                stamp1,
                stamp2,
                stamp3,
                viewModel.ToolRegisterByName,
                System.IO.File.Exists(logoPath) ? logoPath : null,
                partImagePath,
                System.IO.File.Exists(toolSpecsPath) ? toolSpecsPath : null,
                activeLayout);
            var pdfFileName = $"{viewModel.ToolListName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", pdfFileName);
        }
        
        // Handle CSV and TXT formats
        var content = new StringBuilder();
        var separator = formatLower == "csv" ? "," : "\t";
        
        content.AppendLine($"Tool List: {viewModel.ToolListName}");
        content.AppendLine($"Part Number: {viewModel.PartNumber}");
        content.AppendLine($"Part Description: {viewModel.PartDescription}");
        content.AppendLine($"Operation: {viewModel.Operation}");
        content.AppendLine($"Revision: {viewModel.Revision}");
        content.AppendLine($"Project Code: {viewModel.ProjectCode}");
        content.AppendLine($"Machine: {viewModel.MachineName}");
        content.AppendLine($"Workcenter: {viewModel.MachineWorkcenter}");
        content.AppendLine($"Machine Model: {viewModel.MachineModel}");
        content.AppendLine();
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier",
            "Tool Holder", "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)",
            "Tool Corner Radius", "Arbor Description (or equivalent specs)",
            "Tool Path Time in Minutes", "Remarks"
        }));
        
        foreach (var detail in viewModel.Details.Where(d => 
            !string.IsNullOrWhiteSpace(d.ToolNumber) || 
            !string.IsNullOrWhiteSpace(d.ConsumableCode)))
        {
            content.AppendLine(string.Join(separator, new[]
            {
                EscapeField(detail.ToolNumber, separator),
                EscapeField(detail.ToolDescription, separator),
                EscapeField(detail.ConsumableCode, separator),
                EscapeField(detail.Supplier, separator),
                EscapeField(detail.HolderExtensionCode, separator),
                (detail.Diameter ?? 0).ToString("0.##"),
                (detail.FluteLength ?? 0).ToString("0.##"),
                (detail.ProtrusionLength ?? 0).ToString("0.##"),
                (detail.CornerRadius ?? 0).ToString("0.##"),
                EscapeField(detail.ArborCode, separator),
                (detail.ToolPathTimeMinutes ?? 0).ToString("0.##"),
                EscapeField(detail.Remarks, separator)
            }));
        }

        // 3 blank lines of space, then the approval signatures below the table.
        content.AppendLine();
        content.AppendLine();
        content.AppendLine();
        content.AppendLine($"CAM Programmer: {viewModel.CamProgrammer}");
        content.AppendLine($"Approved By: {viewModel.ApprovedBy}");
        
        var fileNameText = $"{viewModel.ToolListName}_{DateTime.Now:yyyyMMdd_HHmmss}";
        var contentType = formatLower switch
        {
            "csv" => "text/csv",
            "txt" => "text/plain",
            _ => "application/vnd.ms-excel"
        };
        var extension = formatLower switch
        {
            "csv" => ".csv",
            "txt" => ".txt",
            _ => ".xls"
        };
        
        return File(Encoding.UTF8.GetBytes(content.ToString()), contentType, fileNameText + extension);
    }
    
    private string EscapeField(string? value, string separator)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (separator == "," && (value.Contains(',') || value.Contains('"')))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
