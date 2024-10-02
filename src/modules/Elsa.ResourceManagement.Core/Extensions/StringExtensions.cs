using System.Globalization;
using System.Text;

namespace Elsa.ResourceManagement;

public static class StringExtensions
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(ResourceItem.ResourceId),
        nameof(ResourceItem.ResourceVersionId),
        nameof(ResourceItem.ResourceType),
        nameof(ResourceItem.Published),
        nameof(ResourceItem.Latest),
        nameof(ResourceItem.ModifiedUtc),
        nameof(ResourceItem.PublishedUtc),
        nameof(ResourceItem.CreatedUtc),
        nameof(ResourceItem.Owner),
        nameof(ResourceItem.Author),
        nameof(ResourceItem.DisplayText),
    };

    /// <summary>
    /// Generates a valid technical name.
    /// </summary>
    /// <remarks>
    /// Uses a white list set of chars.
    /// </remarks>
    public static string ToSafeName(this string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        name = RemoveDiacritics(name);
        name = name.Strip(c => !char.IsLetter(c) && !char.IsDigit(c)).Trim();

        // Don't allow non A-Z chars as first letter, as they are not allowed in prefixes.
        while (name.Length > 0 && !char.IsLetter(name[0]))
            name = name[1..];

        if (name.Length > 128)
            name = name[..128];

        return name;
    }

    public static bool IsReservedResourceName(this string name) => ReservedNames.Contains(name);

    public static string RemoveDiacritics(this string name)
    {
        var stFormD = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var t in stFormD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(t);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(t);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Strip(this string source, Func<char, bool> predicate)
    {
        var result = new char[source.Length];

        var cursor = 0;
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            if (!predicate(current))
            {
                result[cursor++] = current;
            }
        }

        return new string(result, 0, cursor);
    }

    public static string? TrimEnd(this string? value, string trim)
    {
        return value == null
            ? null
            : value.EndsWith(trim, StringComparison.Ordinal)
                ? value[..^trim.Length]
                : value;
    }
}