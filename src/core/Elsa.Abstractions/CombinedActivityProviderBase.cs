using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Microsoft.Extensions.Localization;

namespace Elsa
{
    public abstract class CombinedActivityProviderBase : IActivityProvider, IActivityDesignerProvider
    {
        public IStringLocalizer Localizer { get; }

        protected CombinedActivityProviderBase(IStringLocalizer localizer)
        {
            Localizer = localizer;
        }
        
        async Task<IEnumerable<ActivityDescriptor>> IActivityProvider.DescribeActivitiesAsync(CancellationToken cancellationToken)
        {
            var types = await DescribeAsync(cancellationToken);
            return types.Select(ActivityDescriptor.For);
        }

        async Task<IEnumerable<ActivityDesignerDescriptor>> IActivityDesignerProvider.DescribeActivityDesignersAsync(CancellationToken cancellationToken)
        {
            var types = await DescribeAsync(cancellationToken);
            return types.Select(x => ActivityDesignerDescriptor.For(x, Localizer));
        }
        
        protected virtual Task<IEnumerable<Type>> DescribeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Describe());
        }

        protected virtual IEnumerable<Type> Describe()
        {
            return Enumerable.Empty<Type>();
        }
    }
}