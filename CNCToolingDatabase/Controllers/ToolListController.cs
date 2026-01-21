using Microsoft.AspNetCore.Mvc;
using CNCToolingDatabase.Services;
using System.Text;

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
        
        var content = new StringBuilder();
        var separator = format.ToLower() == "csv" ? "," : "\t";
        
        content.AppendLine(string.Join(separator, new[]
        {
            "Tool List Name", "Part Number", "Operation", "Revision",
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
                EscapeField(item.CreatedBy, separator),
                item.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                EscapeField(item.Status, separator),
                item.LastModifiedDate.ToString("yyyy-MM-dd HH:mm")
            }));
        }
        
        var fileName = $"ToolLists_{DateTime.Now:yyyyMMdd_HHmmss}";
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
