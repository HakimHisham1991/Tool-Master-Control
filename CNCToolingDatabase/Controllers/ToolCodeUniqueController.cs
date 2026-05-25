using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Services;
using System.Text;
using CNCToolingDatabase.Helpers;
using ClosedXML.Excel;

namespace CNCToolingDatabase.Controllers;

public class ToolCodeUniqueController : Controller
{
    private readonly IToolCodeUniqueService _service;
    private readonly ApplicationDbContext _context;

    public ToolCodeUniqueController(IToolCodeUniqueService service, ApplicationDbContext context)
    {
        _service = service;
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? systemToolName,
        string? consumableCode,
        string? supplier,
        string? diameter,
        string? fluteLength,
        string? cornerRadius,
        string? createdDate,
        string? sortColumn,
        string? sortDirection,
        int page = 1,
        int pageSize = 250)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var viewModel = await _service.GetToolCodesAsync(
            search, systemToolName, consumableCode, supplier, diameter, fluteLength, cornerRadius, createdDate, sortColumn, sortDirection, page, pageSize);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string format,
        string? search,
        string? systemToolName,
        string? consumableCode,
        string? supplier,
        string? diameter,
        string? fluteLength,
        string? cornerRadius,
        string? createdDate)
    {
        var viewModel = await _service.GetToolCodesAsync(
            search, systemToolName, consumableCode, supplier, diameter, fluteLength, cornerRadius, createdDate, null, null, 1, int.MaxValue);
        var formatLower = format.ToLower();

        var headers = new[]
        {
            "No.", "System Tool Name", "Consumable Tool Description", "Tool Supplier",
            "Tool Diameter (D1)", "Flute Length (L1)", "Tool Corner Radius",
            "Created Date", "Last Modified"
        };

        if (formatLower == "excel")
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Tool Code Unique");

            int row = 1, colCount = headers.Length;
            ExcelExportHelper.WriteHeaderRow(ws, row, headers);
            row++;
            foreach (var t in viewModel.Tools)
            {
                ws.Cell(row, 1).Value = t.No;
                ws.Cell(row, 2).Value = t.SystemToolName;
                ws.Cell(row, 3).Value = t.ConsumableCode;
                ws.Cell(row, 4).Value = t.Supplier;
                ws.Cell(row, 5).Value = t.Diameter;
                ws.Cell(row, 6).Value = t.FluteLength;
                ws.Cell(row, 7).Value = t.CornerRadius;
                ws.Cell(row, 8).Value = t.CreatedDate;
                ws.Cell(row, 8).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                ws.Cell(row, 9).Value = t.LastModifiedDate;
                ws.Cell(row, 9).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                row++;
            }
            ExcelExportHelper.ApplyTableBorders(ws, 1, row - 1, colCount);
            ExcelExportHelper.AutoFitColumns(ws);

            var fileName = $"ToolCodeUnique_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(ExcelExportHelper.SaveToBytes(workbook), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        var sep = formatLower == "csv" ? "," : "\t";
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(sep, headers));
        foreach (var t in viewModel.Tools)
        {
            sb.AppendLine(string.Join(sep, new[]
            {
                t.No.ToString(),
                Escape(t.SystemToolName, sep),
                Escape(t.ConsumableCode, sep),
                Escape(t.Supplier, sep),
                t.Diameter.ToString("0.##"),
                t.FluteLength.ToString("0.##"),
                t.CornerRadius.ToString("0.##"),
                t.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                t.LastModifiedDate.ToString("yyyy-MM-dd HH:mm")
            }));
        }
        var ext = formatLower == "csv" ? ".csv" : ".txt";
        var ct = formatLower == "csv" ? "text/csv" : "text/plain";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), ct, $"ToolCodeUnique_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
    }

    private static string Escape(string? value, string separator)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (separator == "," && (value.Contains(',') || value.Contains('"')))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    [HttpPost]
    public IActionResult Reset()
    {
        try
        {
            DbSeeder.ResetToolCodeUniques(_context);
            return Json(new { success = true, message = "Master Tool Code Database reset to seed data successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
