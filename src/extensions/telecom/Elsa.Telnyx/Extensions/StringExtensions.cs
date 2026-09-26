using System.Text.RegularExpressions;
using Humanizer;

namespace Elsa.Telnyx.Extensions;

public static class StringExtensions
{
    private static readonly Regex WhiteList = new(@"[^a-zA-Z0-9-_.!~+ ]");
    
    /// <summary>
    /// Sanitizes the caller name.
    /// </summary>
    public static string? SanitizeCallerName(this string? value) => value == null ? null : WhiteList.Replace(value, "").Truncate(128);

    /// <summary>
    /// Returns <code>null</code> if the specifies string is empty.
    /// </summary>
    public static string? EmptyToNull(this string? value) => value is "" ? null : value;
}