using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.ActivityProviders
{
    public class TypedActivityProvider : IActivityProvider
    {
        private readonly IEnumerable<IActivityHandler> handlers;

        public TypedActivityProvider(IEnumerable<IActivityHandler> handlers)
        {
            this.handlers = handlers;
        }
        
        public Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken)
        {
            var descriptors = handlers.Select(x => new ActivityDescriptor
            {
                Name = x.ActivityType.Name,
                DisplayText = x.DisplayText,
                Description = x.Description,
                EndpointsDelegate = x.GetEndpoints
            });
            
            return Task.FromResult(descriptors);
        }
    }
}