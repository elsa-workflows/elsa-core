using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Comparers;
using Flowsharp.Models;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Extensions
{
    public static class ActivityLibraryExtensions
    {
        public static async Task<IEnumerable<ActivityDescriptor>> ListAsync(this IActivityLibrary activityLibrary, Func<ActivityDescriptor, bool> predicate, CancellationToken cancellationToken)
        {
            var descriptors = await activityLibrary.ListAsync(cancellationToken).ToListAsync();
            return descriptors.Where(predicate);
        }

        public static Task<IEnumerable<ActivityDescriptor>> ListBrowsableAsync(this IActivityLibrary activityLibrary, CancellationToken cancellationToken)
        {
            return activityLibrary.ListAsync(x => x.IsBrowsable, cancellationToken);
        }

        public static async Task<IEnumerable<LocalizedString>> GetCategoriesAsync(this IActivityLibrary activityLibrary, CancellationToken cancellationToken)
        {
            var descriptors = await activityLibrary.ListBrowsableAsync(cancellationToken);
            return descriptors
                .Select(x => x.Category)
                .Distinct(new LocalizedStringEqualityComparer())
                .OrderBy(x => x.Value);
        }

        public static Task<ActivityDescriptor> GetByNameAsync(this IActivityLibrary activityLibrary, string name, CancellationToken cancellationToken)
        {
            return activityLibrary
                .ListAsync(x => x.Name == name, cancellationToken)
                .SingleAsync();
        }
    }
}