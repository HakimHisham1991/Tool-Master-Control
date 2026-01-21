using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Models.ViewModels;
using CNCToolingDatabase.Services;
using System.Text;

namespace CNCToolingDatabase.Controllers;

public class ToolListEditorController : Controller
{
    private readonly IToolListService _toolListService;
    
    public ToolListEditorController(IToolListService toolListService)
    {
        _toolListService = toolListService;
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
    public async Task<IActionResult> Close(int id)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        await _toolListService.ReleaseToolListLockAsync(id, username);
        return Json(new { success = true });
    }
    
    [HttpPost]
    public async Task<IActionResult> Heartbeat(int id)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        await _toolListService.UpdateHeartbeatAsync(id, username);
        return Json(new { success = true });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAvailableToolLists(string? search)
    {
        var toolLists = await _toolListService.GetAvailableToolListsAsync(search);
        return Json(toolLists);
    }
    
    [HttpGet]
    public async Task<IActionResult> Export(int id, string format)
    {
        var username = HttpContext.Session.GetString("Username") ?? "";
        var viewModel = await _toolListService.GetToolListForEditAsync(id, username);
        
        var content = new StringBuilder();
        var separator = format.ToLower() == "csv" ? "," : "\t";
        
        content.AppendLine($"Tool List: {viewModel.ToolListName}");
        content.AppendLine($"Part Number: {viewModel.PartNumber}");
        content.AppendLine($"Operation: {viewModel.Operation}");
        content.AppendLine($"Revision: {viewModel.Revision}");
        content.AppendLine($"Project Code: {viewModel.ProjectCode}");
        content.AppendLine($"Machine: {viewModel.MachineName}");
        content.AppendLine($"Workcenter: {viewModel.MachineWorkcenter}");
        content.AppendLine();
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool Number", "Tool Description", "Consumable Code", "Supplier",
            "Holder/Extension Code", "Diameter", "Flute Length", "Protrusion Length",
            "Corner Radius", "Arbor Code"
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
                EscapeField(detail.ArborCode, separator)
            }));
        }
        
        var fileName = $"{viewModel.ToolListName}_{DateTime.Now:yyyyMMdd_HHmmss}";
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
