using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;

namespace Elsa.Api.Client.Activities;

/// <summary>
/// Represents a flowchart activity.
/// </summary>
public class Flowchart : Container
{
    /// <summary>
    /// Gets or sets the connections between activities.
    /// </summary>
    public ICollection<Connection> Connections
    {
        get => this.TryGetValue<ICollection<Connection>>("connections", () => new List<Connection>())!;
        set => this["connections"] = value;
    }
}