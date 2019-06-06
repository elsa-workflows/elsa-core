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
    public static class ActivityDesignerStoreExtensions
    {
        public static async Task<IEnumerable<ActivityDesignerDescriptor>> ListAsync(this IActivityDesignerStore store, Func<ActivityDesignerDescriptor, bool> predicate, CancellationToken cancellationToken)
        {
            var descriptors = await store.ListAsync(cancellationToken).ToListAsync();
            return descriptors.Where(predicate);
        }

        public static Task<IEnumerable<ActivityDesignerDescriptor>> ListBrowsableAsync(this IActivityDesignerStore store, CancellationToken cancellationToken)
        {
            return store.ListAsync(x => x.IsBrowsable, cancellationToken);
        }

        public static async Task<IEnumerable<LocalizedString>> GetCategoriesAsync(this IActivityDesignerStore store, CancellationToken cancellationToken)
        {
            var descriptors = await store.ListBrowsableAsync(cancellationToken);
            return descriptors
                .Select(x => x.Category)
                .Distinct(new LocalizedStringEqualityComparer())
                .OrderBy(x => x.Value);
        }

        public static Task<ActivityDesignerDescriptor> GetByTypeNameAsync(this IActivityDesignerStore store, string name, CancellationToken cancellationToken)
        {
            return store
                .ListAsync(x => x.ActivityTypeName == name, cancellationToken)
                .SingleOrDefaultAsync();
        }
    }
}