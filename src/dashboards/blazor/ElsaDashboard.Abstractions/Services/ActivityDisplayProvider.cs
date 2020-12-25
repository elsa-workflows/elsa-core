using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElsaDashboard.Models;

namespace ElsaDashboard.Services
{
    public abstract class ActivityDisplayProvider : IActivityDisplayProvider
    {
        public virtual ValueTask<IEnumerable<ActivityDisplayDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            var descriptors = GetDescriptors();
            return ValueTask.FromResult(descriptors);
        }
        
        protected virtual IEnumerable<ActivityDisplayDescriptor> GetDescriptors()
        {
            yield break;
        }
    }
}