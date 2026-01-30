namespace CNCToolingDatabase.Models.ViewModels;

/// <summary>ViewModel for the shared _Pagination partial.</summary>
public class PaginationViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; }
    /// <summary>Query string excluding page and pageSize (e.g. "search=foo&amp;sortColumn=Name").</summary>
    public string QueryBase { get; set; } = string.Empty;
    public int[] PageSizeOptions { get; set; } = { 10, 25, 50, 100, 250 };

    public int StartRecord => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int EndRecord => Math.Min(CurrentPage * PageSize, TotalItems);

    /// <summary>Builds page number list for display, with ellipsis for large ranges.</summary>
    public IEnumerable<int> GetPageNumbers()
    {
        if (TotalPages <= 7)
        {
            for (var i = 1; i <= TotalPages; i++) yield return i;
            yield break;
        }
        yield return 1;
        var low = Math.Max(2, CurrentPage - 2);
        var high = Math.Min(TotalPages - 1, CurrentPage + 2);
        if (low > 2) yield return -1; // sentinel for ellipsis
        for (var i = low; i <= high; i++) yield return i;
        if (high < TotalPages - 1) yield return -2; // sentinel for ellipsis
        if (TotalPages > 1) yield return TotalPages;
    }
}
