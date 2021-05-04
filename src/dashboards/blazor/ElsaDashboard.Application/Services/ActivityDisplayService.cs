using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElsaDashboard.Models;
using ElsaDashboard.Services;

namespace ElsaDashboard.Application.Services
{
    public class ActivityDisplayService : IActivityDisplayService
    {
        private readonly IEnumerable<IActivityDisplayProvider> _providers;

        public ActivityDisplayService(IEnumerable<IActivityDisplayProvider> providers)
        {
            _providers = providers;
        }
        
        public async ValueTask<ActivityDisplayDescriptor?> GetDisplayDescriptorAsync(string activityType, CancellationToken cancellationToken = default)
        {
            foreach (var provider in _providers)
            {
                var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
                var descriptor = descriptors.FirstOrDefault(x => x.ActivityType == activityType);

                if (descriptor != null)
                    return descriptor;
            }

            return null;
        }
    }
}