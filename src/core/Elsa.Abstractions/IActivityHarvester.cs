using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public interface IActivityHarvester
    {
        Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken);
    }
}