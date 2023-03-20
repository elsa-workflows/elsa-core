using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

/// <summary>
/// A connection between a source and target activity via the source out port to the target in port.
/// </summary>
public record Connection(IActivity Source, IActivity Target, string? SourcePort = default, string? TargetPort = default);