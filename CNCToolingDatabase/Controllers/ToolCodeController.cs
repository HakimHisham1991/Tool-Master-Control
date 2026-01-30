using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Services;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
        string? consumableCode,
        string? diameter,
        string? arborCode,
        string? holderExtension,
        string? partNumber,
        string? toolListName,
        string? sortColumn,
        string? sortDirection,
        int page = 1,
        int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var viewModel = await _toolCodeService.GetToolCodesAsync(
            search, consumableCode, diameter, arborCode, 
            holderExtension, partNumber, toolListName, sortColumn, sortDirection, 
            page, pageSize);
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Export(
        string format,
        string? search,
        string? consumableCode,
        string? diameter,
        string? arborCode,
        string? holderExtension,
        string? partNumber,
        string? toolListName)
    {
        var viewModel = await _toolCodeService.GetToolCodesAsync(
            search, consumableCode, diameter, arborCode,
            holderExtension, partNumber, toolListName, null, null, 1, int.MaxValue);
        
        var formatLower = format.ToLower();
        
        // Handle Excel format with EPPlus
        if (formatLower == "excel")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Tool Codes");
            
            // Add column headers with color
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
            foreach (var tool in viewModel.Tools)
            {
                worksheet.Cells[row, 1].Value = tool.ToolNumber;
                worksheet.Cells[row, 2].Value = tool.ToolDescription;
                worksheet.Cells[row, 3].Value = tool.ConsumableCode;
                worksheet.Cells[row, 4].Value = tool.Supplier;
                worksheet.Cells[row, 5].Value = tool.HolderExtensionCode;
                worksheet.Cells[row, 6].Value = tool.Diameter;
                worksheet.Cells[row, 7].Value = tool.FluteLength;
                worksheet.Cells[row, 8].Value = tool.ProtrusionLength;
                worksheet.Cells[row, 9].Value = tool.CornerRadius;
                worksheet.Cells[row, 10].Value = tool.ArborCode;
                worksheet.Cells[row, 11].Value = tool.PartNumber;
                worksheet.Cells[row, 12].Value = tool.Operation;
                worksheet.Cells[row, 13].Value = tool.Revision;
                worksheet.Cells[row, 14].Value = tool.ToolListName;
                worksheet.Cells[row, 15].Value = tool.ProjectCode;
                worksheet.Cells[row, 16].Value = tool.MachineName;
                worksheet.Cells[row, 17].Value = tool.MachineWorkcenter;
                worksheet.Cells[row, 18].Value = tool.CreatedBy;
                worksheet.Cells[row, 19].Value = tool.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 20].Value = tool.LastModifiedDate.ToString("yyyy-MM-dd HH:mm");
                row++;
            }
            
            int tableEndRow = row - 1;
            // All borders on table (headers + data)
            for (int r = 1; r <= tableEndRow; r++)
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
            
            var fileName = $"ToolCodes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var fileBytes = package.GetAsByteArray();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
