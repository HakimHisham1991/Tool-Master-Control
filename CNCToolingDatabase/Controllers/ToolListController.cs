using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Services;
using System.Text;
using CNCToolingDatabase.Helpers;
using ClosedXML.Excel;

namespace CNCToolingDatabase.Controllers;

public class ToolListController : Controller
{
    private readonly IToolListService _toolListService;
    private readonly ApplicationDbContext _context;

    public ToolListController(IToolListService toolListService, ApplicationDbContext context)
    {
        _toolListService = toolListService;
        _context = context;
    }
    
    public async Task<IActionResult> Index(
        string? search,
        string? toolListName,
        string? partNumber,
        string? operation,
        string? revision,
        string? numberOfTooling,
        string? projectCode,
        string? machineName,
        string? machineWorkcenter,
        string? machineModel,
        string? sortColumn,
        string? sortDirection,
        int page = 1,
        int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListsAsync(
            search, toolListName, partNumber, operation, revision, numberOfTooling,
            projectCode, machineName, machineWorkcenter, machineModel,
            sortColumn, sortDirection, page, pageSize, username);
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Export(string format, string? search, string? toolListName, string? partNumber, string? operation, string? revision, string? numberOfTooling, string? projectCode, string? machineName, string? machineWorkcenter, string? machineModel)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListsAsync(
            search, toolListName, partNumber, operation, revision, numberOfTooling,
            projectCode, machineName, machineWorkcenter, machineModel,
            null, null, 1, int.MaxValue, username);
        
        var formatLower = format.ToLower();
        
        // Handle Excel format with ClosedXML
        if (formatLower == "excel")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tool Lists");
            
            var headers = new[]
            {
                "Tool List Name", "Part Number", "Operation", "Revision", "No. of Tooling",
                "Project Code", "Machine Name", "Machine Workcenter", "Machine Model",
                "Created By", "Created Date", "Status", "Last Modified Date"
            };
            
            int row = 1;
            int colCount = headers.Length;
            ExcelExportHelper.WriteHeaderRow(worksheet, row, headers);
            row++;
            
            foreach (var item in viewModel.ToolLists)
            {
                worksheet.Cell(row, 1).Value = item.ToolListName;
                worksheet.Cell(row, 2).Value = item.PartNumber;
                worksheet.Cell(row, 3).Value = item.Operation;
                worksheet.Cell(row, 4).Value = item.Revision;
                worksheet.Cell(row, 5).Value = item.NumberOfTooling;
                worksheet.Cell(row, 6).Value = item.ProjectCode;
                worksheet.Cell(row, 7).Value = item.MachineName;
                worksheet.Cell(row, 8).Value = item.MachineWorkcenter;
                worksheet.Cell(row, 9).Value = item.MachineModel;
                worksheet.Cell(row, 10).Value = item.CreatedBy;
                worksheet.Cell(row, 11).Value = item.CreatedDate;
                worksheet.Cell(row, 11).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                worksheet.Cell(row, 12).Value = item.Status;
                worksheet.Cell(row, 13).Value = item.LastModifiedDate;
                worksheet.Cell(row, 13).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                row++;
            }
            
            ExcelExportHelper.ApplyTableBorders(worksheet, 1, row - 1, colCount);
            ExcelExportHelper.AutoFitColumns(worksheet);
            
            var fileName = $"ToolLists_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(ExcelExportHelper.SaveToBytes(workbook), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        
        // Handle CSV and TXT formats
        var content = new StringBuilder();
        var separator = formatLower == "csv" ? "," : "\t";
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool List Name", "Part Number", "Operation", "Revision", "No. of Tooling",
            "Project Code", "Machine Name", "Machine Workcenter", "Machine Model",
            "Created By", "Created Date", "Status", "Last Modified Date"
        }));
        
        foreach (var item in viewModel.ToolLists)
        {
            content.AppendLine(string.Join(separator, new[]
            {
                EscapeField(item.ToolListName, separator),
                EscapeField(item.PartNumber, separator),
                EscapeField(item.Operation, separator),
                EscapeField(item.Revision, separator),
                item.NumberOfTooling.ToString(),
                EscapeField(item.ProjectCode, separator),
                EscapeField(item.MachineName, separator),
                EscapeField(item.MachineWorkcenter, separator),
                EscapeField(item.MachineModel, separator),
                EscapeField(item.CreatedBy, separator),
                item.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                EscapeField(item.Status, separator),
                item.LastModifiedDate.ToString("yyyy-MM-dd HH:mm")
            }));
        }
        
        var fileNameText = $"ToolLists_{DateTime.Now:yyyyMMdd_HHmmss}";
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

    [HttpPost]
    public IActionResult Reset()
    {
        try
        {
            DbSeeder.ResetToolLists(_context);
            return Json(new { success = true, message = "Tool List Database reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
