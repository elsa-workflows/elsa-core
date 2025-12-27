using System.Reflection;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management;

/// <summary>
/// Provides descriptions of workflow activities that are backed by methods on a host type.
/// </summary>
/// <remarks>
/// Implement this interface to translate methods on a host type into <see cref="ActivityDescriptor"/> instances
/// that can be used by the workflow runtime and design-time tooling. Implementations are responsible for
/// inspecting the specified host type and methods, and returning descriptors that describe how they should be
/// represented and configured as workflow activities.
/// </remarks>
public interface IHostMethodActivityDescriber
{
    /// <summary>
    /// Describes all workflow activities exposed by the specified host type for the given key.
    /// </summary>
    /// <param name="key">A classifier used to select or group host methods (for example, a category or provider key).</param>
    /// <param name="hostType">The host type whose methods should be described as workflow activities.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that, when completed, contains the collection of <see cref="ActivityDescriptor"/> instances
    /// describing the applicable host methods.
    /// </returns>
    Task<IEnumerable<ActivityDescriptor>> DescribeAsync(string key, Type hostType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes a single workflow activity backed by the specified host method.
    /// </summary>
    /// <param name="key">A classifier used to select or group host methods (for example, a category or provider key).</param>
    /// <param name="hostType">The host type that declares the method to describe.</param>
    /// <param name="method">The method that should be described as a workflow activity.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that, when completed, contains the <see cref="ActivityDescriptor"/> describing the specified method.
    /// </returns>
    Task<ActivityDescriptor> DescribeMethodAsync(string key, Type hostType, MethodInfo method, CancellationToken cancellationToken = default);
}
