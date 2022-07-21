using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Represents a provider of activity descriptors, which can be used from activity pickers.
/// </summary>
public interface IActivityProvider
{
    ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}