namespace Elsa.Identity.Extensions;

public static class CollectionExtensions
{
    public static ICollection<T> AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
        foreach (var item in source) target.Add(item);
        return target;
    }
}