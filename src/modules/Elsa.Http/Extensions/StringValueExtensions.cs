using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="StringValues"/> Enumerable dictonaries.
/// </summary>
public static class StringValueExtensions
{
    /// <summary>
    /// Convert the collection to the desired dictionary type.
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static Dictionary<string, object> ToObjectDictionary(this IEnumerable<KeyValuePair<string, StringValues>> collection)
    {
        return collection.ToDictionary<KeyValuePair<string, StringValues>, string, object>(
            item => item.Key,
            item => item.Value.Count <= 1 ?
            item.Value[0]!
            : item.Value.ToArray());
    }
}
