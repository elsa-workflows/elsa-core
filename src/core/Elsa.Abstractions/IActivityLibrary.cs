using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public interface IActivityLibrary
    {
        Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken);
    }
}