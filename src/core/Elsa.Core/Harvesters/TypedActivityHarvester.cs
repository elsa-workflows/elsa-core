using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Harvesters
{
    public class TypedActivityHarvester : IActivityHarvester
    {
        private readonly IEnumerable<IActivityProvider> providers;

        public TypedActivityHarvester(IEnumerable<IActivityProvider> providers)
        {
            this.providers = providers;
        }
        
        public async Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken)
        {
            var descriptorLists = await Task.WhenAll(providers.Select(x => x.DescribeActivitiesAsync(cancellationToken)));
            return descriptorLists.SelectMany(x => x);
        }
    }
}