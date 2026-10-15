namespace Elsa.Identity.Endpoints.Me.Permissions;

/// <summary>The calling principal's effective grants for the current tenant context.</summary>
/// <param name="Grants">
/// Every registered resource, including ones the caller cannot access, which carry an empty verb list.
/// Present so a client can distinguish "explicitly denied" from "unknown to this server" and drive
/// rendering safely from a single call.
/// </param>
public record Response(IReadOnlyCollection<ResourceGrant> Grants);

/// <summary>What the caller may do with one resource.</summary>
/// <param name="Verbs">
/// Resolved to concrete verbs rather than echoing a wildcard, so a client needs no matching logic: the
/// check is a containment test.
/// </param>
public record ResourceGrant(string Resource, IReadOnlyCollection<string> Verbs);
