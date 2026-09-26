namespace Elsa.Studio.UIHints.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Trims the whitespace from the string, including null characters and new lines.
    /// </summary>
    public static string TrimWhitespace(this string value)
    {
        return value.Trim(' ', '\0', '\n', '\r');
    }
    
    /// <summary>
    /// Trims the null characters from the string.
    /// </summary>
    public static string TrimNull(this string value)
    {
        return value.Trim('\0');
    }
}