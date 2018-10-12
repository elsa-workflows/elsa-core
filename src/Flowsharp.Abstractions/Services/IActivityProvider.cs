using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Descriptors;

namespace Flowsharp.Services
{
    /// <summary>
    /// Implementors provide available activity descriptors.
    /// </summary>
    public interface IActivityProvider
    {
        Task<IEnumerable<ActivityDescriptor>> GetActivityDescriptorsAsync(CancellationToken cancellationToken);
    }
}
