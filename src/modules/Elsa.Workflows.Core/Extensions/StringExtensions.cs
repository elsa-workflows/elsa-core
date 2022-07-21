namespace Elsa.Workflows.Core;

public static class StringExtensions
{
    public static string WithDefault(this string? value, string defaultValue) => !string.IsNullOrWhiteSpace(value) ? value : defaultValue;
}