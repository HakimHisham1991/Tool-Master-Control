using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Services;
using System.Text;

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
        string? sortColumn,
        string? sortDirection,
        int page = 1)
    {
        var viewModel = await _toolCodeService.GetToolCodesAsync(
            search, consumableCode, diameter, arborCode, 
            holderExtension, partNumber, sortColumn, sortDirection, 
            page, 100);
        
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
        string? partNumber)
    {
        var viewModel = await _toolCodeService.GetToolCodesAsync(
            search, consumableCode, diameter, arborCode,
            holderExtension, partNumber, null, null, 1, int.MaxValue);
        
        var content = new StringBuilder();
        var separator = format.ToLower() == "csv" ? "," : "\t";
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool Number", "Tool Description", "Consumable Code", "Supplier",
            "Holder/Extension Code", "Diameter", "Flute Length", "Protrusion Length",
            "Corner Radius", "Arbor Code", "Part Number", "Operation", "Revision",
            "Project Code", "Machine Name", "Machine Workcenter"
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
                EscapeField(tool.ProjectCode, separator),
                EscapeField(tool.MachineName, separator),
                EscapeField(tool.MachineWorkcenter, separator)
            }));
        }
        
        var fileName = $"ToolCodes_{DateTime.Now:yyyyMMdd_HHmmss}";
        var contentType = format.ToLower() switch
        {
            "csv" => "text/csv",
            "txt" => "text/plain",
            _ => "application/vnd.ms-excel"
        };
        var extension = format.ToLower() switch
        {
            "csv" => ".csv",
            "txt" => ".txt",
            _ => ".xls"
        };
        
        return File(Encoding.UTF8.GetBytes(content.ToString()), contentType, fileName + extension);
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
