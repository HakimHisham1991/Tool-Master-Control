using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Services;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CNCToolingDatabase.Controllers;

public class ToolListController : Controller
{
    private readonly IToolListService _toolListService;
    
    public ToolListController(IToolListService toolListService)
    {
        _toolListService = toolListService;
    }
    
    public async Task<IActionResult> Index(
        string? search,
        string? sortColumn,
        string? sortDirection,
        int page = 1)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListsAsync(
            search, sortColumn, sortDirection, page, 100, username);
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> Export(string format, string? search)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListsAsync(
            search, null, null, 1, int.MaxValue, username);
        
        var formatLower = format.ToLower();
        
        // Handle Excel format with EPPlus
        if (formatLower == "excel")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Tool Lists");
            
            // Add column headers with color
            var headers = new[]
            {
                "Tool List Name", "Part Number", "Operation", "Revision", "No. of Tooling",
                "Created By", "Created Date", "Status", "Last Modified Date"
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
            foreach (var item in viewModel.ToolLists)
            {
                worksheet.Cells[row, 1].Value = item.ToolListName;
                worksheet.Cells[row, 2].Value = item.PartNumber;
                worksheet.Cells[row, 3].Value = item.Operation;
                worksheet.Cells[row, 4].Value = item.Revision;
                worksheet.Cells[row, 5].Value = item.NumberOfTooling;
                worksheet.Cells[row, 6].Value = item.CreatedBy;
                worksheet.Cells[row, 7].Value = item.CreatedDate;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                worksheet.Cells[row, 8].Value = item.Status;
                worksheet.Cells[row, 9].Value = item.LastModifiedDate;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
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
            
            var fileName = $"ToolLists_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var fileBytes = package.GetAsByteArray();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        
        // Handle CSV and TXT formats
        var content = new StringBuilder();
        var separator = formatLower == "csv" ? "," : "\t";
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool List Name", "Part Number", "Operation", "Revision", "No. of Tooling",
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
}
