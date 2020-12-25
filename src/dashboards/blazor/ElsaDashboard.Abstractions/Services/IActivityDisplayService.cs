using System.Threading;
using System.Threading.Tasks;
using ElsaDashboard.Models;

namespace ElsaDashboard.Services
{
    public interface IActivityDisplayService
    {
        ValueTask<ActivityDisplayDescriptor?> GetDisplayDescriptorAsync(string activityType, CancellationToken cancellationToken = default);
    }
}