using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public abstract class ActivityProviderBase : IActivityProvider
    {
        public Task<IEnumerable<ActivityDescriptor>> DescribeActivitiesAsync(CancellationToken cancellationToken)
        {
            return DescribeAsync(cancellationToken);
        }

        protected virtual Task<IEnumerable<ActivityDescriptor>> DescribeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Describe());
        }

        protected virtual IEnumerable<ActivityDescriptor> Describe()
        {
            return Enumerable.Empty<ActivityDescriptor>();
        }
    }
}