namespace Elsa.Common.Multitenancy;

/// <summary>
/// Options for activating tenants.
/// </summary>
public class TenantActivationOptions
{
    /// <summary>
    /// The number of tenants that are activated at the same time. An activation runs every startup task of its tenant,
    /// each of which typically claims a database connection, so this also bounds how many connections activation holds.
    /// Use -1 for no limit; otherwise the value must be at least 1. Defaults to 1, which activates tenants one after another.
    /// Values above 1 run the tenant-activated handlers and startup tasks of different tenants concurrently, so custom ones must be thread-safe.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;
}
