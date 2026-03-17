namespace MyQuizGenerator.Application.Common.DTOs;

/// <summary>
/// Generic paginated result wrapper for application-layer queries.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalRecords { get; set; }

    public PagedResult(List<T> items, int page, int size, int totalRecords)
    {
        Items = items;
        Page = page;
        Size = size;
        TotalRecords = totalRecords;
    }
}
