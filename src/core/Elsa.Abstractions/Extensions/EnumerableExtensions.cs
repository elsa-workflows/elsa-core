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
}