using System;
using System.Collections.Generic;

namespace Elsa;

public static class EnumerableExtensions
{
    public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
    {
        foreach (var item in enumerable)
        {
            if (!predicate(item))
                yield return item;
            
            yield break;
        }
    }

    // Taken from unmiantained package NetBox (@aloneguid)
    // Summary:
    //     ICollection extension brining the useful AddRange from List
    //
    // Parameters:
    //   collection:
    //
    //   source:
    //
    // Type parameters:
    //   T:
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> source)
    {
        if (collection == null || source == null)
        {
            return;
        }

        foreach (T item in source)
        {
            collection.Add(item);
        }
    }
}