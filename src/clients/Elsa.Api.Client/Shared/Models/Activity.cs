using Elsa.Api.Client.Extensions;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents an activity in a workflow definition.
/// </summary>
public class Activity : Dictionary<string, object>
{
    /// <summary>
    /// Gets or sets the ID of this activity.
    /// </summary>
    public string Id
    {
        get => this.TryGetValue<string>("id")!;
        set => this["id"] = value;
    }

    /// <summary>
    /// Gets or sets the type of this activity.
    /// </summary>
    public string Type
    {
        get => this.TryGetValue<string>("type")!;
        set => this["type"] = value;
    }

    /// <summary>
    /// Gets or sets the version of this activity.
    /// </summary>
    public int Version
    {
        get => this.TryGetValue<int>("version");
        set => this["version"] = value;
    }

    /// <summary>
    /// Gets or sets the metadata of this activity.
    /// </summary>
    public IDictionary<string, object> Metadata
    {
        get => this.TryGetValue<IDictionary<string, object>>("metadata", () => new Dictionary<string, object>())!;
        set => this["metadata"] = value;
    }

    /// <summary>
    /// Gets or sets custom properties of this activity.
    /// </summary>
    public IDictionary<string, object> CustomProperties
    {
        get => this.TryGetValue<IDictionary<string, object>>("customProperties", () => new Dictionary<string, object>())!;
        set => this["customProperties"] = value;
    }
}