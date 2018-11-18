using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivityProvider
    {
        Task<IEnumerable<Models.IActivity>> GetActivitiesAsync(CancellationToken cancellationToken);
    }
}