using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Descriptors;

namespace Flowsharp.Services
{
    public class ActivityLibrary : IActivityLibrary
    {
        private readonly IEnumerable<IActivityProvider> providers;

        public ActivityLibrary(IEnumerable<IActivityProvider> providers)
        {
            this.providers = providers;
        }

        public async Task<IEnumerable<ActivityDescriptor>> GetActivityDescriptorsAsync(CancellationToken cancellationToken)
        {
            var tasks = providers.Select(x => x.GetActivityDescriptorsAsync(cancellationToken)).ToList();
            var results = await Task.WhenAll(tasks);

            return results.SelectMany(x => x);
        }
    }

    public static class ActivityLibraryExtensions
    {
        public static async Task<IDictionary<string, ActivityDescriptor>> GetActivityDescriptorDictionaryAsync(this IActivityLibrary activityLibrary, CancellationToken cancellationToken)
        {
            var activityDescriptors = await activityLibrary.GetActivityDescriptorsAsync(cancellationToken);
            return activityDescriptors.ToDictionary(x => x.Name);
        }
    }
}
