using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a connection between two activities.
/// </summary>
public class Endpoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Endpoint"/> class.
    /// </summary>
    [JsonConstructor]
    public Endpoint()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Endpoint"/> class.
    /// </summary>
    /// <param name="activityId">The activity identifier.</param>
    /// <param name="port">The port.</param>
    public Endpoint(string activityId, string? port = default)
    {
        ActivityId = activityId;
        Port = port;
    }
    
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    [JsonPropertyName("activity")]
    public string ActivityId { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public string? Port { get; set; }
}