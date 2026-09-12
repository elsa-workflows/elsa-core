namespace Elsa.Workflows;

/// <summary>
/// Marks an activity provider whose descriptors do not depend on the current tenant.
/// </summary>
/// <remarks>
/// Descriptors from a tenant-agnostic provider can be initialized once for each
/// <see cref="IActivityRegistry"/> instance.
/// </remarks>
public interface ITenantAgnosticActivityProvider : IActivityProvider;
