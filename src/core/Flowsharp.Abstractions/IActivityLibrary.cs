using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp
{
    public interface IActivityLibrary
    {
        Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken);
    }
}