using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Data;
using CNCToolingDatabase.Services;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
        string? consumableCode,
        string? supplier,
        string? sortColumn,
        string? sortDirection,
        int page = 1,
        int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 10, 250);
        var viewModel = await _service.GetToolCodesAsync(
            search, consumableCode, supplier, sortColumn, sortDirection, page, pageSize);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string format,
        string? search,
        string? consumableCode,
        string? supplier)
    {
        var viewModel = await _service.GetToolCodesAsync(
            search, consumableCode, supplier, null, null, 1, int.MaxValue);
        var formatLower = format.ToLower();

        var headers = new[]
        {
            "No.", "System Tool Name", "Consumable Tool Description", "Tool Supplier",
            "Tool Diameter (D1)", "Flute Length (L1)", "Tool Corner Radius",
            "Created Date", "Last Modified"
        };

        if (formatLower == "excel")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Tool Code Unique");

            int row = 1, colCount = headers.Length;
            for (int c = 1; c <= colCount; c++)
            {
                var cell = ws.Cells[row, c];
                cell.Value = headers[c - 1];
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(204, 255, 255));
                cell.Style.Font.Bold = true;
            }
            row++;
            foreach (var t in viewModel.Tools)
            {
                ws.Cells[row, 1].Value = t.No;
                ws.Cells[row, 2].Value = t.SystemToolName;
                ws.Cells[row, 3].Value = t.ConsumableCode;
                ws.Cells[row, 4].Value = t.Supplier;
                ws.Cells[row, 5].Value = t.Diameter;
                ws.Cells[row, 6].Value = t.FluteLength;
                ws.Cells[row, 7].Value = t.CornerRadius;
                ws.Cells[row, 8].Value = t.CreatedDate;
                ws.Cells[row, 8].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                ws.Cells[row, 9].Value = t.LastModifiedDate;
                ws.Cells[row, 9].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                row++;
            }
            int endRow = row - 1;
            for (int r = 1; r <= endRow; r++)
                for (int c = 1; c <= colCount; c++)
                {
                    var cell = ws.Cells[r, c];
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
            if (ws.Dimension != null)
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var fileName = $"ToolCodeUnique_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
