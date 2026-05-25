using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Services;
using System.Text;
using CNCToolingDatabase.Helpers;
using ClosedXML.Excel;

namespace CNCToolingDatabase.Controllers;

public class ToolCodeController : Controller
{
    private readonly IToolCodeService _toolCodeService;
    
    public ToolCodeController(IToolCodeService toolCodeService)
    {
        _toolCodeService = toolCodeService;
    }
    
    public async Task<IActionResult> Index(
        string? search,
        string? toolNumber,
        string? toolDescription,
        string? consumableCode,
        string? supplier,
        string? holderExtension,
        string? diameter,
        string? fluteLength,
        string? protrusionLength,
        string? cornerRadius,
        string? arborCode,
        string? partNumber,
        string? operation,
        string? revision,
        string? toolListName,
        string? projectCode,
        string? machineName,
        string? machineWorkcenter,
        string? createdBy,
        string? sortColumn,
        string? sortDirection,
        int page = 1,
        int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var viewModel = await _toolCodeService.GetToolCodesAsync(
            search, toolNumber, toolDescription, consumableCode, supplier,
            holderExtension, diameter, fluteLength, protrusionLength, cornerRadius,
            arborCode, partNumber, operation, revision, toolListName,
            projectCode, machineName, machineWorkcenter, createdBy,
            sortColumn, sortDirection, page, pageSize);
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Export(
        string format,
        string? search,
        string? toolNumber,
        string? toolDescription,
        string? consumableCode,
        string? supplier,
        string? holderExtension,
        string? diameter,
        string? fluteLength,
        string? protrusionLength,
        string? cornerRadius,
        string? arborCode,
        string? partNumber,
        string? operation,
        string? revision,
        string? toolListName,
        string? projectCode,
        string? machineName,
        string? machineWorkcenter,
        string? createdBy)
    {
        var viewModel = await _toolCodeService.GetToolCodesAsync(
            search, toolNumber, toolDescription, consumableCode, supplier,
            holderExtension, diameter, fluteLength, protrusionLength, cornerRadius,
            arborCode, partNumber, operation, revision, toolListName,
            projectCode, machineName, machineWorkcenter, createdBy,
            null, null, 1, int.MaxValue);
        
        var formatLower = format.ToLower();
        
        // Handle Excel format with ClosedXML
        if (formatLower == "excel")
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tool Codes");
            
            var headers = new[]
            {
                "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier",
                "Tool Holder", "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)",
                "Tool Corner Radius", "Arbor Description (or equivalent specs)", "Part Number", "Operation", "Revision",
                "Tool List Name", "Project Code", "Machine Name", "Machine Workcenter",
                "Created By", "Created Date", "Last Modified"
            };
            
            int row = 1;
            int colCount = headers.Length;
            ExcelExportHelper.WriteHeaderRow(worksheet, row, headers);
            row++;
            
            foreach (var tool in viewModel.Tools)
            {
                worksheet.Cell(row, 1).Value = tool.ToolNumber;
                worksheet.Cell(row, 2).Value = tool.ToolDescription;
                worksheet.Cell(row, 3).Value = tool.ConsumableCode;
                worksheet.Cell(row, 4).Value = tool.Supplier;
                worksheet.Cell(row, 5).Value = tool.HolderExtensionCode;
                worksheet.Cell(row, 6).Value = tool.Diameter;
                worksheet.Cell(row, 7).Value = tool.FluteLength;
                worksheet.Cell(row, 8).Value = tool.ProtrusionLength;
                worksheet.Cell(row, 9).Value = tool.CornerRadius;
                worksheet.Cell(row, 10).Value = tool.ArborCode;
                worksheet.Cell(row, 11).Value = tool.PartNumber;
                worksheet.Cell(row, 12).Value = tool.Operation;
                worksheet.Cell(row, 13).Value = tool.Revision;
                worksheet.Cell(row, 14).Value = tool.ToolListName;
                worksheet.Cell(row, 15).Value = tool.ProjectCode;
                worksheet.Cell(row, 16).Value = tool.MachineName;
                worksheet.Cell(row, 17).Value = tool.MachineWorkcenter;
                worksheet.Cell(row, 18).Value = tool.CreatedBy;
                worksheet.Cell(row, 19).Value = tool.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 20).Value = tool.LastModifiedDate.ToString("yyyy-MM-dd HH:mm");
                row++;
            }
            
            ExcelExportHelper.ApplyTableBorders(worksheet, 1, row - 1, colCount);
            ExcelExportHelper.AutoFitColumns(worksheet);
            
            var fileName = $"ToolCodes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(ExcelExportHelper.SaveToBytes(workbook), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        
        // Handle CSV and TXT formats
        var content = new StringBuilder();
        var separator = formatLower == "csv" ? "," : "\t";
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool No.", "Tool Name", "Consumable Tool Description", "Tool Supplier",
            "Tool Holder", "Tool Diameter (D1)", "Flute Length (L1)", "Tool Ext. Length (L2)",
            "Tool Corner Radius", "Arbor Description (or equivalent specs)", "Part Number", "Operation", "Revision",
            "Tool List Name", "Project Code", "Machine Name", "Machine Workcenter",
            "Created By", "Created Date", "Last Modified"
        }));
        
        foreach (var tool in viewModel.Tools)
        {
            content.AppendLine(string.Join(separator, new[]
            {
                EscapeField(tool.ToolNumber, separator),
                EscapeField(tool.ToolDescription, separator),
                EscapeField(tool.ConsumableCode, separator),
                EscapeField(tool.Supplier, separator),
                EscapeField(tool.HolderExtensionCode, separator),
                tool.Diameter.ToString("0.##"),
                tool.FluteLength.ToString("0.##"),
                tool.ProtrusionLength.ToString("0.##"),
                tool.CornerRadius.ToString("0.##"),
                EscapeField(tool.ArborCode, separator),
                EscapeField(tool.PartNumber, separator),
                EscapeField(tool.Operation, separator),
                EscapeField(tool.Revision, separator),
                EscapeField(tool.ToolListName, separator),
                EscapeField(tool.ProjectCode, separator),
                EscapeField(tool.MachineName, separator),
                EscapeField(tool.MachineWorkcenter, separator),
                EscapeField(tool.CreatedBy, separator),
                tool.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                tool.LastModifiedDate.ToString("yyyy-MM-dd HH:mm")
            }));
        }
        
        var fileNameText = $"ToolCodes_{DateTime.Now:yyyyMMdd_HHmmss}";
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
