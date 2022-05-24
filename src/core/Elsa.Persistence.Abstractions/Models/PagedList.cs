namespace Elsa.Persistence.Common.Models;

public record Page<T>(ICollection<T> Items, long TotalCount);

public static class Page
{
    public static Page<T> Of<T>(ICollection<T> items, long totalCount) => new(items, totalCount);
}