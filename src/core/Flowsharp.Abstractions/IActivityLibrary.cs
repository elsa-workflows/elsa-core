using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivityLibrary
    {
        Task<IEnumerable<Models.ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken);
        Task<Models.ActivityDescriptor> GetActivityByNameAsync(string name, CancellationToken cancellationToken);
    }
}