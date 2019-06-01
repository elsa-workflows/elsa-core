using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public interface IActivityStore
    {
        Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken);
    }
}