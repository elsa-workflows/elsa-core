using System.Collections.Generic;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class ActivityCollectionExtensions
    {
        public static IEnumerable<IActivity> Find(this IEnumerable<IActivity> collection, IEnumerable<string> ids)
        {
            var idList = ids?.ToList();
            return idList != null ? collection.Where(x => idList.Contains(x.Id)) : default;
        }
    }
}