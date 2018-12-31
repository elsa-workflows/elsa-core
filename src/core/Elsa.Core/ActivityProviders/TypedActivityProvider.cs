using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.ActivityProviders
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
                IsBrowsable = x.ActivityType.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true,
                IsTrigger = x.IsTrigger,
                Category = x.Category,
                ActivityType = x.ActivityType,
                DisplayText = x.DisplayText,
                Description = x.Description,
                GetEndpoints = x.GetEndpoints,
                CanExecuteAsync = x.CanExecuteAsync,
                ExecuteAsync = x.ExecuteAsync,
                ResumeAsync = x.ResumeAsync
            });
            
            return Task.FromResult(descriptors);
        }
    }
}