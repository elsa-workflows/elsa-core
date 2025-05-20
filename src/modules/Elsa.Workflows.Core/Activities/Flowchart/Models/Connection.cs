using System.Text.Json.Serialization;

namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// A connection between a source and a target endpoint.
/// </summary>
public class Connection : IEquatable<Connection>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    [JsonConstructor]
    public Connection()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="source">The source endpoint.</param>
    /// <param name="target">The target endpoint.</param>
    public Connection(Endpoint source, Endpoint target)
    {
        Source = source;
        Target = target;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="source">The source endpoint.</param>
    /// <param name="target">The target endpoint.</param>
    public Connection(IActivity source, IActivity target)
    {
        Source = new(source);
        Target = new(target);
    }

    /// <summary>
    /// The source endpoint.
    /// </summary>
    public Endpoint Source { get; set; } = null!;
    
    /// <summary>
    /// The target endpoint.
    /// </summary>
    public Endpoint Target { get; set; } = null!;

    /// <summary>
    /// A collection of points representing the vertices of the connection.
    /// </summary>
    public ICollection<Position> Vertices { get; set; } = [];

    public override string ToString() => 
        $"{Source.Activity.Id}{(string.IsNullOrEmpty(Source.Port) ? "" : $":{Source.Port}")}->" +
        $"{Target.Activity.Id}{(string.IsNullOrEmpty(Target.Port) ? "" : $":{Target.Port}")}";

    // Implement equality logic
    public bool Equals(Connection? other)
    {
        if (other == null) return false;
        return AreEndpointsEqual(Source, other.Source) && AreEndpointsEqual(Target, other.Target);
    }

    public override bool Equals(object? obj)
    {
        return obj is Connection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetEndpointHashCode(Source), GetEndpointHashCode(Target));
    }

    private static bool AreEndpointsEqual(Endpoint e1, Endpoint e2)
    {
        return e1.Activity.Equals(e2.Activity) && e1.Port == e2.Port;
    }

    private static int GetEndpointHashCode(Endpoint endpoint)
    {
        return HashCode.Combine(endpoint.Activity.GetHashCode(), endpoint.Port?.GetHashCode() ?? 0);
    }
}