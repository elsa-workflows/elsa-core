using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public class ActivityDesignerStore : IActivityDesignerStore
    {
        private readonly IEnumerable<IActivityDesignerProvider> providers;

        public ActivityDesignerStore(IEnumerable<IActivityDesignerProvider> providers)
        {
            this.providers = providers;
        }
        
        public async Task<IEnumerable<ActivityDesignerDescriptor>> ListAsync(CancellationToken cancellationToken)
        {
            var designers = await Task.WhenAll(providers.Select(async x => await x.DescribeActivityDesignersAsync(cancellationToken)));
            return designers.SelectMany(x => x);
        }
    }
}