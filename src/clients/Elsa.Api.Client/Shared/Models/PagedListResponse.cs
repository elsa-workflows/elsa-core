namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a generic paged list response that offers a unified format for returning paged list of things from API endpoints.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public class PagedListResponse<T> : LinkedEntity
{
    public ICollection<T> Items { get; set; } = default!;
    public long TotalCount { get; set; }
}