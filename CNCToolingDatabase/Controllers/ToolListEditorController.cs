using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNCToolingDatabase.Models.ViewModels;
using CNCToolingDatabase.Services;
using CNCToolingDatabase.Data;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CNCToolingDatabase.Controllers;

public class ToolListEditorController : Controller
{
    private readonly IToolListService _toolListService;
    private readonly ApplicationDbContext _context;
    
    public ToolListEditorController(IToolListService toolListService, ApplicationDbContext context)
    {
        _toolListService = toolListService;
        _context = context;
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
        return Json(new { success = true, approvedByUserId = userId.Value, approvedDateFormatted });
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
        return Json(new { success = true, approvedByUserId = userId.Value, approvedDateFormatted });
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
    
    [HttpGet]
    public async Task<IActionResult> Export(int id, string format)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListForEditAsync(id, username);
        
        var formatLower = format.ToLower();
        
        // Handle Excel format with EPPlus
        if (formatLower == "excel")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Tool List");
            
            // Add header information
            int row = 1;
            worksheet.Cells[row, 1].Value = "Tool List:";
            worksheet.Cells[row, 2].Value = viewModel.ToolListName;
            row++;
            worksheet.Cells[row, 1].Value = "Part Number:";
            worksheet.Cells[row, 2].Value = viewModel.PartNumber;
            row++;
            worksheet.Cells[row, 1].Value = "Operation:";
            worksheet.Cells[row, 2].Value = viewModel.Operation;
            row++;
            worksheet.Cells[row, 1].Value = "Revision:";
            worksheet.Cells[row, 2].Value = viewModel.Revision;
            row++;
            worksheet.Cells[row, 1].Value = "Project Code:";
            worksheet.Cells[row, 2].Value = viewModel.ProjectCode;
            row++;
            worksheet.Cells[row, 1].Value = "Machine:";
            worksheet.Cells[row, 2].Value = viewModel.MachineName;
            row++;
            worksheet.Cells[row, 1].Value = "Workcenter:";
            worksheet.Cells[row, 2].Value = viewModel.MachineWorkcenter;
            row++;
            worksheet.Cells[row, 1].Value = "Machine Model:";
            worksheet.Cells[row, 2].Value = viewModel.MachineModel;
            row++;
            worksheet.Cells[row, 1].Value = "Approved By:";
            worksheet.Cells[row, 2].Value = viewModel.ApprovedBy;
            row++;
            worksheet.Cells[row, 1].Value = "CAM Programmer:";
            worksheet.Cells[row, 2].Value = viewModel.CamProgrammer;
            row++;
            worksheet.Cells[row, 1].Value = "General Name:";
            worksheet.Cells[row, 2].Value = viewModel.Material;
            row += 2;
            
            // Add column headers with color
            var headers = new[]
            {
                "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier",
                "Tool Holder", "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)",
                "Tool Corner Radius", "Arbor Description (or equivalent specs)",
                "Tool Path Time in Minutes", "Remarks"
            };
            
            int tableStartRow = row;
            int colCount = headers.Length;
            for (int col = 1; col <= colCount; col++)
            {
                var cell = worksheet.Cells[row, col];
                cell.Value = headers[col - 1];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(204, 255, 255)); // RGB(204,255,255) = #CCFFFF
                cell.Style.Font.Bold = true;
            }
            row++;
            
            // Add data rows
            foreach (var detail in viewModel.Details.Where(d => 
                !string.IsNullOrWhiteSpace(d.ToolNumber) || 
                !string.IsNullOrWhiteSpace(d.ConsumableCode)))
            {
                worksheet.Cells[row, 1].Value = detail.ToolNumber;
                worksheet.Cells[row, 2].Value = detail.ToolDescription;
                worksheet.Cells[row, 3].Value = detail.ConsumableCode;
                worksheet.Cells[row, 4].Value = detail.Supplier;
                worksheet.Cells[row, 5].Value = detail.HolderExtensionCode;
                worksheet.Cells[row, 6].Value = detail.Diameter ?? 0;
                worksheet.Cells[row, 7].Value = detail.FluteLength ?? 0;
                worksheet.Cells[row, 8].Value = detail.ProtrusionLength ?? 0;
                worksheet.Cells[row, 9].Value = detail.CornerRadius ?? 0;
                worksheet.Cells[row, 10].Value = detail.ArborCode;
                worksheet.Cells[row, 11].Value = detail.ToolPathTimeMinutes ?? 0;
                worksheet.Cells[row, 12].Value = detail.Remarks ?? string.Empty;
                row++;
            }
            
            int tableEndRow = row - 1;
            // All borders on table (headers + data)
            for (int r = tableStartRow; r <= tableEndRow; r++)
                for (int c = 1; c <= colCount; c++)
                {
                    var cell = worksheet.Cells[r, c];
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
            
            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            var fileName = $"{viewModel.ToolListName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var fileBytes = package.GetAsByteArray();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        
        // Handle PDF format
        if (formatLower == "pdf")
        {
            var details = viewModel.Details
                .Where(d => !string.IsNullOrWhiteSpace(d.ToolNumber) || !string.IsNullOrWhiteSpace(d.ConsumableCode))
                .ToList();
            // Fetch stamp images and display names for the three approval sections
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
            var arialNarrow = "Arial Narrow";
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Data", "LOGO", "ZENIX.png");
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily(arialNarrow).FontSize(9));
                    page.Header().Row(headerRow =>
                    {
                        headerRow.ConstantItem(80).Element(e =>
                        {
                            if (System.IO.File.Exists(logoPath))
                                e.Image(logoPath).FitWidth();
                        });
                        headerRow.RelativeItem().Column(column =>
                        {
                            column.Item().AlignCenter().Text("Master Tooling List").Bold().FontFamily(arialNarrow).FontSize(22).FontColor(Colors.Black);
                            column.Item().Height(8);
                            column.Item().Text($"Tool List No.: {viewModel.ToolListName}").FontFamily(arialNarrow).FontSize(9);
                            column.Item().Text($"Unit: MM").FontFamily(arialNarrow).FontSize(9);
                            column.Item().Text($"Part Number: {viewModel.PartNumber}").FontFamily(arialNarrow).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(viewModel.PartDescription))
                                column.Item().Text($"Part Description: {viewModel.PartDescription}").FontFamily(arialNarrow).FontSize(9);
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Machine: {viewModel.MachineName}").FontFamily(arialNarrow);
                                row.RelativeItem().Text($"Workcenter: {viewModel.MachineWorkcenter}").FontFamily(arialNarrow);
                                row.RelativeItem().Text($"Project Code: {viewModel.ProjectCode}").FontFamily(arialNarrow);
                            });
                        });
                    });
                    page.Content().PaddingTop(0.5f, Unit.Centimetre).Column(content =>
                    {
                        // Table with exact headers from Create/Edit Tool List page
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(45);  // Tool No.
                                columns.RelativeColumn(2);   // Tool Name
                                columns.RelativeColumn(2);   // Consumable Tool Description
                                columns.RelativeColumn(1.2f);// Tool Supplier
                                columns.RelativeColumn(1.2f);// Tool Holder
                                columns.ConstantColumn(40);  // Tool Diameter (D1)
                                columns.ConstantColumn(40);  // Flute Length (L1)
                                columns.ConstantColumn(40);  // Tool Ext. Length (L2)
                                columns.ConstantColumn(45);  // Tool Corner Radius
                                columns.RelativeColumn(2);   // Arbor Description
                                columns.ConstantColumn(55);  // Tool Path Time in Minutes
                                columns.RelativeColumn();    // Remarks
                            });
                            var headerColor = "#CCFFFF";
                            var borderThin = 0.5f;
                            var borderColor = Colors.Black;
                            var headers = new[]
                            {
                                "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier", "Tool Holder",
                                "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)", "Tool Corner Radius",
                                "Arbor Description (or equivalent specs)", "Tool Path Time in Minutes", "Remarks"
                            };
                            table.Header(header =>
                            {
                                foreach (var h in headers)
                                    header.Cell().Border(borderThin).BorderColor(borderColor).Background(headerColor).Padding(4).AlignCenter().AlignMiddle().Text(h).Bold().FontFamily(arialNarrow).FontSize(6);
                            });
                            foreach (var d in details)
                            {
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.ToolNumber ?? "").FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.ToolDescription ?? "").FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.ConsumableCode ?? "").FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.Supplier ?? "").FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.HolderExtensionCode ?? "").FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text((d.Diameter ?? 0).ToString("0.##")).FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text((d.FluteLength ?? 0).ToString("0.##")).FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text((d.ProtrusionLength ?? 0).ToString("0.##")).FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text((d.CornerRadius ?? 0).ToString("0.##")).FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.ArborCode ?? "").FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text((d.ToolPathTimeMinutes ?? 0).ToString("0.##")).FontFamily(arialNarrow).FontSize(6);
                                table.Cell().Border(borderThin).BorderColor(borderColor).Padding(3).AlignCenter().AlignMiddle().Text(d.Remarks ?? "").FontFamily(arialNarrow).FontSize(6);
                            }
                        });
                        // Stamp section (50% size: 55x55, no borders)
                        content.Item().PaddingTop(1f, Unit.Centimetre).PaddingTop(0.5f, Unit.Centimetre).Row(stampRow =>
                            {
                                stampRow.ConstantItem(95).Column(c =>
                                {
                                    c.Item().Text("CAM Programmer:").Bold().FontFamily(arialNarrow).FontSize(9);
                                    c.Item().PaddingTop(4).Width(55).Height(55).Background(Colors.White)
                                        .AlignCenter().AlignMiddle().Element(e =>
                                        {
                                            if (stamp1 != null && stamp1.Length > 0)
                                                e.Image(stamp1).FitArea();
                                        });
                                    if (viewModel.ApprovedDate.HasValue)
                                        c.Item().PaddingTop(2).Text(viewModel.ApprovedDate.Value.ToString("dd/MM/yyyy")).FontFamily(arialNarrow).FontSize(8);
                                });
                                stampRow.ConstantItem(20);
                                stampRow.ConstantItem(95).Column(c =>
                                {
                                    c.Item().Text("Approved by:").Bold().FontFamily(arialNarrow).FontSize(9);
                                    c.Item().PaddingTop(4).Width(55).Height(55).Background(Colors.White)
                                        .AlignCenter().AlignMiddle().Element(e =>
                                        {
                                            if (stamp2 != null && stamp2.Length > 0)
                                                e.Image(stamp2).FitArea();
                                        });
                                    if (viewModel.CamLeaderApprovedDate.HasValue)
                                        c.Item().PaddingTop(2).Text(viewModel.CamLeaderApprovedDate.Value.ToString("dd/MM/yyyy")).FontFamily(arialNarrow).FontSize(8);
                                });
                                stampRow.ConstantItem(20);
                                stampRow.ConstantItem(95).Column(c =>
                                {
                                    c.Item().Text("Tool Register By:").Bold().FontFamily(arialNarrow).FontSize(9);
                                    c.Item().PaddingTop(4).Width(55).Height(55).Background(Colors.White)
                                        .AlignCenter().AlignMiddle().Element(e =>
                                        {
                                            if (stamp3 != null && stamp3.Length > 0)
                                                e.Image(stamp3).FitArea();
                                        });
                                    if (viewModel.ToolRegisterByDate.HasValue)
                                        c.Item().PaddingTop(2).Text(viewModel.ToolRegisterByDate.Value.ToString("dd/MM/yyyy")).FontFamily(arialNarrow).FontSize(8);
                                });
                            });
                    });
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontFamily(arialNarrow));
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });
            var pdfBytes = document.GeneratePdf();
            var pdfFileName = $"{viewModel.ToolListName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", pdfFileName);
        }
        
        // Handle CSV and TXT formats
        var content = new StringBuilder();
        var separator = formatLower == "csv" ? "," : "\t";
        
        content.AppendLine($"Tool List: {viewModel.ToolListName}");
        content.AppendLine($"Part Number: {viewModel.PartNumber}");
        content.AppendLine($"Operation: {viewModel.Operation}");
        content.AppendLine($"Revision: {viewModel.Revision}");
        content.AppendLine($"Project Code: {viewModel.ProjectCode}");
        content.AppendLine($"Machine: {viewModel.MachineName}");
        content.AppendLine($"Workcenter: {viewModel.MachineWorkcenter}");
        content.AppendLine($"Machine Model: {viewModel.MachineModel}");
        content.AppendLine($"General Name: {viewModel.Material}");
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
