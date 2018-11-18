using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivityProvider
    {
        Task<IEnumerable<Models.ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken);
    }
}