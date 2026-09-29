namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// Represents the result of paged.
/// </summary>
public record PagedResult<T>(ICollection<T> Items, long TotalCount);