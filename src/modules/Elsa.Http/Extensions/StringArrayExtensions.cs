// ReSharper disable once CheckNamespace

namespace Elsa.Extensions;

public static class StringArrayExtensions
{
    public static string Join(this string?[] segments, string separator = "/")
    {
        return string.Join(separator, segments.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim('/')));
    }
}