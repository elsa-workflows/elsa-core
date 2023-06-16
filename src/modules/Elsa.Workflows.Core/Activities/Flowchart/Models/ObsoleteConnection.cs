using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

/// <summary>
/// A connection between a source and target activity via the source out port to the target in port.
/// </summary>
[Obsolete("Use Connection instead.")]
public class ObsoleteConnection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObsoleteConnection"/> class.
    /// </summary>
    [JsonConstructor]
    public ObsoleteConnection()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ObsoleteConnection"/> class.
    /// </summary>
    public ObsoleteConnection(IActivity source, IActivity target, string? sourcePort = default, string? targetPort = default)
    {
        Source = source;
        Target = target;
        SourcePort = sourcePort;
        TargetPort = targetPort;
    }
    
    /// <summary>
    /// The source activity.
    /// </summary>
    public IActivity Source { get; set; } = default!;
    
    /// <summary>
    /// The target activity.
    /// </summary>
    public IActivity Target { get; set; } = default!;
    
    /// <summary>
    /// The source port.
    /// </summary>
    public string? SourcePort { get; set; }
    
    /// <summary>
    /// The target port.
    /// </summary>
    public string? TargetPort { get; set; }

    /// <summary>
    /// Deconstructs the connection into its parts.
    /// </summary>
    public void Deconstruct(out IActivity source, out IActivity target, out string? sourcePort, out string? targetPort)
    {
        source = Source;
        target = Target;
        sourcePort = SourcePort;
        targetPort = TargetPort;
    }
}