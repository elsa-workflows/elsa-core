using System.Text.RegularExpressions;
using Humanizer;

namespace Elsa.Activities.Telnyx.Extensions;

public static class StringExtensions
{
    private static readonly Regex WhiteList = new(@"[^a-zA-Z0-9-_.!~+ ]");
    
    public static string? SanitizeCallerName(this string? value) => value == null ? null : WhiteList.Replace(value, "").Truncate(128);
}