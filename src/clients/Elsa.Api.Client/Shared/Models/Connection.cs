namespace Elsa.Api.Client.Shared.Models;

/// <summary>
/// Represents a connection between two activities.
/// </summary>
public class Connection
{
    /// <summary>
    /// Gets or sets the source endpoint.
    /// </summary>
    public Endpoint Source { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target endpoint.
    /// </summary>
    public Endpoint Target { get; set; } = null!;

    /// <summary>
    /// Gets or sets the vertices of the connection.
    /// </summary>
    public ICollection<Position> Vertices { get; set; } = [];
}