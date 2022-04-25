using Elsa.Management.Models;

namespace Elsa.Management.Services;

public interface IActivityProvider
{
    ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}