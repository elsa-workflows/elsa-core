using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Descriptors;

namespace Flowsharp.Services
{
    public interface IActivityLibrary
    {
        Task<IEnumerable<ActivityDescriptor>> GetActivityDescriptorsAsync(CancellationToken cancellationToken);
    }
}
