namespace Elsa.Workflows.Runtime;

/// <summary>
/// Point-in-time view of a registered ingress source, surfaced via <c>IIngressSourceRegistry.Snapshot()</c>
/// and the admin status endpoint.
/// </summary>
public sealed record IngressSourceSnapshot(
    string Name,
    IngressSourceState State,
    Exception? LastError,
    DateTimeOffset? LastTransitionAt);
