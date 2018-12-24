using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Comparers;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Extensions
{
    public static class ActivityLibraryExtensions
    {
        public static async Task<IEnumerable<LocalizedString>> GetCategoriesAsync(this IActivityLibrary activityLibrary, CancellationToken cancellationToken)
        {
            var descriptors = await activityLibrary.GetActivitiesAsync(cancellationToken);
            return descriptors.Select(x => x.Category).Distinct(new LocalizedStringEqualityComparer()).OrderBy(x => x.Value);
        }
    }
}