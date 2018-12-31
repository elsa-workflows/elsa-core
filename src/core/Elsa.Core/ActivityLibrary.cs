using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public class ActivityLibrary : IActivityLibrary
    {
        private readonly IEnumerable<IActivityProvider> providers;

        public ActivityLibrary(IEnumerable<IActivityProvider> providers)
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