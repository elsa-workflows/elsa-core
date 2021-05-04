using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElsaDashboard.Models;

namespace ElsaDashboard.Services
{
    public interface IActivityDisplayProvider
    {
        ValueTask<IEnumerable<ActivityDisplayDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
    }
}