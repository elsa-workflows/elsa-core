using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.ActivityProviders
{
    public class TypedActivityHarvester : IActivityHarvester
    {
        private readonly IEnumerable<IActivityDescriptor> handlers;

        public TypedActivityHarvester(IEnumerable<IActivityDescriptor> handlers)
        {
            this.handlers = handlers;
        }
        
        public Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken)
        {
            var descriptors = handlers.Select(x => new ActivityDescriptor
            {
                Name = x.ActivityType.Name,
                IsBrowsable = x.ActivityType.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true,
                IsTrigger = x.IsTrigger,
                Category = x.Category,
                ActivityType = x.ActivityType,
                DisplayText = x.DisplayText,
                Description = x.Description,
                GetEndpoints = x.GetEndpoints
            });
            
            return Task.FromResult(descriptors);
        }
    }
}