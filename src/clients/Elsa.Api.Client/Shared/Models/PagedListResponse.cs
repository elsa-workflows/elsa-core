namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a generic paged list response that offers a unified format for returning paged list of things from API endpoints.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public class PagedListResponse<T> : LinkedEntity
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ICollection<T> Items { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public long TotalCount { get; set; }
}