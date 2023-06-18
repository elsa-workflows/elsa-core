using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

/// <summary>
/// A connection between a source and a target endpoint.
/// </summary>
public class Connection
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
        Source = new Endpoint(source);
        Target = new Endpoint(target);
    }

    /// <summary>
    /// The source endpoint.
    /// </summary>
    public Endpoint Source { get; set; } = default!;
    
    /// <summary>
    /// The target endpoint.
    /// </summary>
    public Endpoint Target { get; set; } = default!;
}