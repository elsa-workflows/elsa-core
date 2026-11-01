namespace Elsa.Common.Multitenancy;

/// <summary>
/// Options for activating tenants.
/// </summary>
public class TenantActivationOptions
{
    /// <summary>
    /// The number of tenants that are activated at the same time. An activation runs every startup task of its tenant,
    /// each of which typically claims a database connection, so this also bounds how many connections activation holds.
    /// Use -1 for no limit; otherwise the value must be at least 1. Defaults to 4.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;
}
