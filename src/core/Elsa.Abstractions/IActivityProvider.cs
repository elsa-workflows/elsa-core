using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public interface IActivityProvider
    {
        Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CancellationToken cancellationToken);
    }
}