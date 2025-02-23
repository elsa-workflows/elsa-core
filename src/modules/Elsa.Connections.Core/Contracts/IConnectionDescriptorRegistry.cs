using Elsa.Connections.Models;
using Elsa.Workflows.Models;

namespace Elsa.Connections.Contracts;

/// <summary>
/// Store all connection descriptors available to the system
/// </summary>
public interface IConnectionDescriptorRegistry
{
    /// <summary>
    /// Adds a connection descriptor to the registry
    /// </summary>
    /// <param name="connectionType">The type of the connection </param>
    /// <param name="connectionDescriptor">The type desiptor of the connection</param>
    void Add(Type connectionType, ConnectionDescriptor connectionDescriptor);

    /// <summary>
    /// Removes an activity descriptor from the registry.
    /// </summary>
    /// <param name="connectionType">The type of the connection.</param>
    /// <param name="descriptor"></param>
    void Remove(Type connectionType, ActivityDescriptor descriptor);

    /// <summary>
    /// Returns all connection descriptors in the registry.
    /// </summary>
    /// <returns>All connection descriptors in the registry.</returns>
    IEnumerable<ConnectionDescriptor> ListAll();

    Type? Get(string type);

    /// <summary>
    /// Get the Input Descriptors for an connection
    /// </summary>
    /// <param name="activityType">The type of the connectn</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<InputDescriptor>> GetConnectionDescriptorAsync(string activityType, CancellationToken cancellationToken = default);
}