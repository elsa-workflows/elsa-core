using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides a list of activity descriptors to the activity registry. This is a way to abstract the source of activity descriptors.
/// </summary>
public interface IActivityRegistryProvider
{
    /// <summary>
    /// Returns a list of activity descriptors.
    /// </summary>
    Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}