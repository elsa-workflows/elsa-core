using System.Collections;
using System.DirectoryServices.Protocols;

namespace Elsa.Ldap.Extensions;

internal static class SearchResultEntryExtensions
{
    /// <summary>
    /// Maps a <see cref="SearchResultEntry"/> to a dictionary of attribute names to string arrays.
    /// </summary>
    internal static Dictionary<string, string[]> MapToAttributesDictionary(this SearchResultEntry entry)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry de in entry.Attributes)
        {
            var attribute = (DirectoryAttribute)de.Value!;
            var values = new string[attribute.Count];

            for (var i = 0; i < attribute.Count; i++)
            {
                values[i] = attribute[i]?.ToString() ?? string.Empty;
            }

            result[attribute.Name] = values;
        }

        return result;
    }
}
