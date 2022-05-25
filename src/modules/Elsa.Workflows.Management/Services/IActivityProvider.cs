using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

public interface IActivityProvider
{
    ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}