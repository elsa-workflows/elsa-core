using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public abstract class ActivityDesignerProviderBase : IActivityDesignerProvider
    {
        public Task<IEnumerable<ActivityDesignerDescriptor>> DescribeActivityDesignersAsync(CancellationToken cancellationToken)
        {
            return DescribeAsync(cancellationToken);
        }
        
        protected virtual Task<IEnumerable<ActivityDesignerDescriptor>> DescribeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Describe());
        }

        protected virtual IEnumerable<ActivityDesignerDescriptor> Describe()
        {
            return Enumerable.Empty<ActivityDesignerDescriptor>();
        }
    }
}