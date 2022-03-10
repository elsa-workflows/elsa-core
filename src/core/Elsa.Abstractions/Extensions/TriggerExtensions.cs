using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

namespace Elsa;

public static class TriggerExtensions
{
    public static IEnumerable<Trigger> FilterByBookmarkType<T>(this IEnumerable<Trigger> triggers) where T:IBookmark
    {
        var typeName = typeof(T).GetSimpleAssemblyQualifiedName();
        return triggers.Where(x => x.ModelType == typeName);
    }
}