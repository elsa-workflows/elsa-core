using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public class ActivityStore : IActivityStore
    {
        private readonly IEnumerable<IActivityHarvester> providers;

        public ActivityStore(IEnumerable<IActivityHarvester> providers)
        {
            this.providers = providers;
        }

        public async Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken)
        {
            var tasks = providers.Select(x => x.GetActivitiesAsync(cancellationToken));
            var descriptorsList = await Task.WhenAll(tasks);
            return descriptorsList.SelectMany(x => x);
        }
    }
}