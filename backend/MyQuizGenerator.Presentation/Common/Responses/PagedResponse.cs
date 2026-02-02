using System;
using System.Collections.Generic;

namespace MyQuizGenerator.Presentation.Common.Responses;

/// <summary>
/// Response wrapper for paginated data
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PagedResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Pagination metadata
    /// </summary>
    public PaginationMeta? Pagination { get; set; }

    /// <summary>
    /// Creates a successful paged response
    /// </summary>
    public static PagedResponse<T> Create(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalRecords,
        string message = "Success")
    {
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        return new PagedResponse<T>
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            Data = data,
            Pagination = new PaginationMeta
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasPrevious = pageNumber > 1,
                HasNext = pageNumber < totalPages
            }
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of records
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNext { get; set; }
}
