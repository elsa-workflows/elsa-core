using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Represents a provider of activity descriptors, which can be used from activity pickers.
/// </summary>
public interface IActivityProvider
{
    /// <summary>
    /// Returns a list of <see cref="ActivityDescriptor"/> objects.
    /// </summary>
    ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default);
}