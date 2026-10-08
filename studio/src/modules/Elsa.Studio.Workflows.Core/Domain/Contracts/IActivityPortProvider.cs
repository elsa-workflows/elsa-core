using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides ports for activities.
/// </summary>
public interface IActivityPortProvider
{
    /// <summary>
    /// Gets the priority of the provider.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// Returns true if the provider supports the specified activity type.
    /// </summary>
    bool GetSupportsActivityType(PortProviderContext context);
    
    /// <summary>
    /// Returns the ports for the specified activity.
    /// </summary>
    /// <param name="context">The context.</param>
    IEnumerable<Port> GetPorts(PortProviderContext context);
    
    /// <summary>
    /// Returns the activity for the specified port.
    /// </summary>
    /// <param name="portName">The name of the port.</param>
    /// <param name="context">The context.</param>
    JsonObject? ResolvePort(string portName, PortProviderContext context);
    
    /// <summary>
    /// Assigns the specified activity to the specified port.
    /// </summary>
    /// <param name="portName">The name of the port.</param>
    /// <param name="activity">The activity to assign.</param>
    /// <param name="context">The context.</param>
    void AssignPort(string portName, JsonObject activity, PortProviderContext context);

    /// <summary>
    /// Clears the specified port.
    /// </summary>
    /// <param name="portName">The name of the port.</param>
    /// <param name="context">The context.</param>
    void ClearPort(string portName, PortProviderContext context);
}