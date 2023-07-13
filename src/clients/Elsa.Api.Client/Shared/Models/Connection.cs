namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a connection between two activities.
/// </summary>
public class Connection
{
    /// <summary>
    /// Gets or sets the source endpoint.
    /// </summary>
    public Endpoint Source { get; set; } = default!;

    /// <summary>
    /// Gets or sets the target endpoint.
    /// </summary>
    public Endpoint Target { get; set; } = default!;
}