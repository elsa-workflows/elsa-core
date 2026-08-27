namespace Elsa.Workflows.Models;

/// <summary>
/// Describes the declared destination of an output binding.
/// </summary>
public sealed record OutputBindingDestination(
    string Id,
    Type Type,
    bool AllowsNull,
    OutputBindingDestinationKind Kind);
