using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.ActivityProviders
{
    public interface IActivityTypeProvider
    {
        ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default);
    }
}