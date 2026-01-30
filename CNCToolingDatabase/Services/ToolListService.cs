using CNCToolingDatabase.Models;
using CNCToolingDatabase.Models.ViewModels;
using CNCToolingDatabase.Repositories;

namespace CNCToolingDatabase.Services;

public class ToolListService : IToolListService
{
    private readonly IToolListRepository _toolListRepository;
    private readonly IToolMasterRepository _toolMasterRepository;
    
    public ToolListService(IToolListRepository toolListRepository, IToolMasterRepository toolMasterRepository)
    {
        _toolListRepository = toolListRepository;
        _toolMasterRepository = toolMasterRepository;
    }
    
    public async Task<ToolListDatabaseViewModel> GetToolListsAsync(
        string? searchTerm,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize,
        string currentUsername)
    {
        // Reduced timeout from 5 minutes to 1 minute to match shorter heartbeat interval
        await _toolListRepository.ReleaseExpiredLocksAsync(TimeSpan.FromMinutes(1));
        
        var headers = await _toolListRepository.GetAllHeadersAsync();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            headers = headers.Where(h => 
                h.ToolListName.ToLower().Contains(term) ||
                h.PartNumber.ToLower().Contains(term) ||
                h.Operation.ToLower().Contains(term) ||
                h.CreatedBy.ToLower().Contains(term)).ToList();
        }

        headers = await ApplySortAsync(headers, sortColumn, sortDirection);
        
        var totalItems = headers.Count;
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var pagedHeaders = headers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var headerIds = pagedHeaders.Select(h => h.Id).ToList();
        var toolCounts = await _toolListRepository.GetToolCountsByHeaderIdsAsync(headerIds);
        
        var toolLists = pagedHeaders
            .Select(h => new ToolListItemViewModel
            {
                Id = h.Id,
                ToolListName = h.ToolListName,
                PartNumber = h.PartNumber,
                Operation = h.Operation,
                Revision = h.Revision,
                NumberOfTooling = toolCounts.GetValueOrDefault(h.Id, 0),
                CreatedBy = h.CreatedBy,
                CreatedDate = h.CreatedDate,
                LastModifiedDate = h.LastModifiedDate,
                LockedBy = h.LockedBy,
                Status = h.LockedBy == null ? "Available" : $"Locked by {h.LockedBy}",
                CanEdit = h.LockedBy == null || h.LockedBy == currentUsername,
                EditingDuration = h.LockStartTime.HasValue 
                    ? FormatDuration(DateTime.UtcNow - h.LockStartTime.Value) 
                    : null
            }).ToList();
        
        return new ToolListDatabaseViewModel
        {
            ToolLists = toolLists,
            SearchTerm = searchTerm,
            SortColumn = sortColumn,
            SortDirection = sortDirection,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }
    
    public async Task<ToolListEditorViewModel> GetToolListForEditAsync(int id, string username)
    {
        var header = await _toolListRepository.GetHeaderWithDetailsAsync(id);
        if (header == null)
        {
            return new ToolListEditorViewModel { ErrorMessage = "Tool list not found" };
        }
        
        var lockAcquired = await _toolListRepository.AcquireLockAsync(id, username);
        var isReadOnly = !lockAcquired && header.LockedBy != username;
        
        var details = header.Details.Select(d => new ToolListDetailRow
        {
            Id = d.Id,
            ToolNumber = d.ToolNumber,
            ToolDescription = d.ToolDescription,
            ConsumableCode = d.ConsumableCode,
            Supplier = d.Supplier,
            HolderExtensionCode = d.HolderExtensionCode,
            Diameter = d.Diameter,
            FluteLength = d.FluteLength,
            ProtrusionLength = d.ProtrusionLength,
            CornerRadius = d.CornerRadius,
            ArborCode = d.ArborCode,
            ToolPathTimeMinutes = d.ToolPathTimeMinutes,
            Remarks = d.Remarks
        }).ToList();
        
        while (details.Count < 5)
        {
            details.Add(new ToolListDetailRow());
        }
        
        return new ToolListEditorViewModel
        {
            Id = header.Id,
            ToolListName = header.ToolListName,
            PartNumber = header.PartNumber,
            Operation = header.Operation,
            Revision = header.Revision,
            ProjectCode = header.ProjectCode,
            MachineName = header.MachineName,
            MachineWorkcenter = header.MachineWorkcenter,
            MachineModel = header.MachineModel,
            ApprovedBy = header.ApprovedBy ?? "",
            CamProgrammer = header.CamProgrammer ?? "",
            Details = details,
            IsReadOnly = isReadOnly,
            LockedBy = isReadOnly ? header.LockedBy : null
        };
    }
    
    public Task<ToolListEditorViewModel> CreateNewToolListAsync()
    {
        var details = new List<ToolListDetailRow>();
        for (int i = 0; i < 5; i++)
        {
            details.Add(new ToolListDetailRow());
        }
        
        return Task.FromResult(new ToolListEditorViewModel
        {
            Details = details,
            Revision = "REV00"
        });
    }
    
    public async Task<(bool Success, string Message, int? Id)> SaveToolListAsync(SaveToolListRequest request, string username)
    {
        try
        {
            ToolListHeader header;
            
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                header = await _toolListRepository.GetHeaderByIdAsync(request.Id.Value) 
                    ?? throw new Exception("Tool list not found");
                
                if (header.LockedBy != null && header.LockedBy != username)
                {
                    return (false, $"Tool list is locked by {header.LockedBy}", null);
                }
                
                header.PartNumber = request.PartNumber;
                header.Operation = request.Operation;
                header.Revision = request.Revision;
                header.ProjectCode = request.ProjectCode;
                header.MachineName = request.MachineName;
                header.MachineWorkcenter = request.MachineWorkcenter;
                header.MachineModel = request.MachineModel;
                header.ApprovedBy = request.ApprovedBy ?? "";
                header.CamProgrammer = request.CamProgrammer ?? "";
                
                await _toolListRepository.UpdateHeaderAsync(header);
            }
            else
            {
                header = new ToolListHeader
                {
                    PartNumber = request.PartNumber,
                    Operation = request.Operation,
                    Revision = request.Revision,
                    ProjectCode = request.ProjectCode,
                    MachineName = request.MachineName,
                    MachineWorkcenter = request.MachineWorkcenter,
                    MachineModel = request.MachineModel,
                    ApprovedBy = request.ApprovedBy ?? "",
                    CamProgrammer = request.CamProgrammer ?? "",
                    CreatedBy = username,
                    CreatedDate = DateTime.UtcNow
                };
                
                header = await _toolListRepository.CreateHeaderAsync(header);
                await _toolListRepository.AcquireLockAsync(header.Id, username);
            }
            
            var validDetails = request.Details
                .Where(d => !string.IsNullOrWhiteSpace(d.ToolNumber) || 
                           !string.IsNullOrWhiteSpace(d.ConsumableCode))
                .Select(d => new ToolListDetail
                {
                    ToolNumber = d.ToolNumber,
                    ToolDescription = d.ToolDescription,
                    ConsumableCode = d.ConsumableCode,
                    Supplier = d.Supplier,
                    HolderExtensionCode = d.HolderExtensionCode,
                    Diameter = d.Diameter ?? 0,
                    FluteLength = d.FluteLength ?? 0,
                    ProtrusionLength = d.ProtrusionLength ?? 0,
                    CornerRadius = d.CornerRadius ?? 0,
                    ArborCode = d.ArborCode,
                    ToolPathTimeMinutes = d.ToolPathTimeMinutes ?? 0,
                    Remarks = d.Remarks ?? string.Empty
                }).ToList();
            
            await _toolListRepository.SaveDetailsAsync(header.Id, validDetails);
            
            foreach (var detail in validDetails)
            {
                await _toolMasterRepository.UpdateFromDetailAsync(detail);
            }
            
            return (true, "Tool list saved successfully", header.Id);
        }
        catch (Exception ex)
        {
            return (false, $"Error saving tool list: {ex.Message}", null);
        }
    }
    
    public async Task<bool> ReleaseToolListLockAsync(int id, string username)
    {
        return await _toolListRepository.ReleaseLockAsync(id, username);
    }
    
    public async Task UpdateHeartbeatAsync(int id, string username)
    {
        await _toolListRepository.UpdateHeartbeatAsync(id, username);
    }
    
    public async Task<List<ToolListItemViewModel>> GetAvailableToolListsAsync(string? searchTerm)
    {
        var headers = await _toolListRepository.GetAllHeadersAsync();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            headers = headers.Where(h => 
                h.ToolListName.ToLower().Contains(term) ||
                h.PartNumber.ToLower().Contains(term)).ToList();
        }
        
        return headers.Select(h => new ToolListItemViewModel
        {
            Id = h.Id,
            ToolListName = h.ToolListName,
            PartNumber = h.PartNumber,
            Operation = h.Operation,
            Revision = h.Revision
        }).ToList();
    }
    
    private async Task<List<ToolListHeader>> ApplySortAsync(List<ToolListHeader> headers, string? column, string? direction)
    {
        var isDescending = direction?.ToLower() == "desc";

        var columnLower = column?.ToLower();

        // Special cases need data beyond the basic header fields
        if (columnLower == "numberoftooling")
        {
            // Need counts for all headers to sort correctly before paging
            var allHeaderIds = headers.Select(h => h.Id).ToList();
            var toolCounts = await _toolListRepository.GetToolCountsByHeaderIdsAsync(allHeaderIds);

            return isDescending
                ? headers.OrderByDescending(h => toolCounts.GetValueOrDefault(h.Id, 0)).ThenByDescending(h => h.LastModifiedDate).ToList()
                : headers.OrderBy(h => toolCounts.GetValueOrDefault(h.Id, 0)).ThenByDescending(h => h.LastModifiedDate).ToList();
        }

        if (columnLower == "status")
        {
            // Available (Unlocked) vs Locked; secondarily sort by who locked it.
            // Put locked entries first when descending, available first when ascending.
            if (isDescending)
            {
                return headers
                    .OrderByDescending(h => h.LockedBy != null)
                    .ThenBy(h => h.LockedBy ?? string.Empty)
                    .ThenByDescending(h => h.LastModifiedDate)
                    .ToList();
            }

            return headers
                .OrderBy(h => h.LockedBy != null)
                .ThenBy(h => h.LockedBy ?? string.Empty)
                .ThenByDescending(h => h.LastModifiedDate)
                .ToList();
        }

        if (columnLower == "editingduration")
        {
            // Duration is based on LockStartTime (null => not currently being edited).
            // Keep unlocked rows at the end, regardless of direction.
            var now = DateTime.UtcNow;

            Func<ToolListHeader, double> durationSeconds = h =>
                h.LockStartTime.HasValue ? (now - h.LockStartTime.Value).TotalSeconds : 0d;

            return isDescending
                ? headers
                    .OrderBy(h => !h.LockStartTime.HasValue)
                    .ThenByDescending(durationSeconds)
                    .ThenByDescending(h => h.LastModifiedDate)
                    .ToList()
                : headers
                    .OrderBy(h => !h.LockStartTime.HasValue)
                    .ThenBy(durationSeconds)
                    .ThenByDescending(h => h.LastModifiedDate)
                    .ToList();
        }

        return columnLower switch
        {
            "id" => isDescending
                ? headers.OrderByDescending(h => h.Id).ToList()
                : headers.OrderBy(h => h.Id).ToList(),
            "toollistname" => isDescending
                ? headers.OrderByDescending(h => h.ToolListName).ToList()
                : headers.OrderBy(h => h.ToolListName).ToList(),
            "partnumber" => isDescending
                ? headers.OrderByDescending(h => h.PartNumber).ToList()
                : headers.OrderBy(h => h.PartNumber).ToList(),
            "operation" => isDescending
                ? headers.OrderByDescending(h => h.Operation).ToList()
                : headers.OrderBy(h => h.Operation).ToList(),
            "revision" => isDescending
                ? headers.OrderByDescending(h => h.Revision).ToList()
                : headers.OrderBy(h => h.Revision).ToList(),
            "createdby" => isDescending
                ? headers.OrderByDescending(h => h.CreatedBy).ToList()
                : headers.OrderBy(h => h.CreatedBy).ToList(),
            "createddate" => isDescending
                ? headers.OrderByDescending(h => h.CreatedDate).ToList()
                : headers.OrderBy(h => h.CreatedDate).ToList(),
            "lastmodifieddate" => isDescending
                ? headers.OrderByDescending(h => h.LastModifiedDate).ToList()
                : headers.OrderBy(h => h.LastModifiedDate).ToList(),
            _ => headers.OrderByDescending(h => h.LastModifiedDate).ToList()
        };
    }
    
    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m";
        return "< 1m";
    }
}
