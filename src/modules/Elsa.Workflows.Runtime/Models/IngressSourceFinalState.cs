namespace Elsa.Workflows.Runtime;

/// <summary>
/// Terminal state of a single ingress source as recorded in a <see cref="DrainOutcome"/>.
/// </summary>
public sealed record IngressSourceFinalState(
    string Name,
    IngressSourceState State,
    Exception? LastError,
    bool WasForceStopped);
