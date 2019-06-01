using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Comparers;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa.Extensions
{
    public static class ActivityStoreExtensions
    {
        public static async Task<IEnumerable<ActivityDescriptor>> ListAsync(this IActivityStore activityStore, Func<ActivityDescriptor, bool> predicate, CancellationToken cancellationToken)
        {
            var descriptors = await activityStore.ListAsync(cancellationToken).ToListAsync();
            return descriptors.Where(predicate);
        }

        public static Task<ActivityDescriptor> GetByNameAsync(this IActivityStore activityStore, string name, CancellationToken cancellationToken)
        {
            return activityStore
                .ListAsync(x => x.ActivityTypeName == name, cancellationToken)
                .SingleOrDefaultAsync();
        }
    }
}