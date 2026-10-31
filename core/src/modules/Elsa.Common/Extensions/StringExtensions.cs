// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class StringExtensions
{
    public static string WithDefault(this string? value, string defaultValue) => !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
    public static string EmptyIfNull(this string? value) => value ?? "";
    public static string? NullIfEmpty(this string? value) => value == "" ? null : value;
    public static string? NullIfWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}