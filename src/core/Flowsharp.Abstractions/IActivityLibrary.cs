using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivityLibrary
    {
        Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken);
        Task<ActivityDescriptor> GetActivityByNameAsync(string name, CancellationToken cancellationToken);
    }
}