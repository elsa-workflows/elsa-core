namespace Elsa.MongoDb.Extensions;

public static class StringExtensions
{
    public static string? EmptyToNull(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}