using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivityLibrary
    {
        Task<IEnumerable<Models.IActivity>> GetActivitiesAsync(CancellationToken cancellationToken);
        Task<Models.IActivity> GetActivityByNameAsync(string name, CancellationToken cancellationToken);
    }
}