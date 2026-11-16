using Elsa.Common.Multitenancy;

namespace Elsa.Tenants.Options;

/// <summary>
/// Options for configuring the Tenants.
/// </summary>
public class MultitenancyOptions
{
    /// <summary>
    /// Gets or sets the tenant resolution pipeline builder.
    /// </summary>
    public ITenantResolverPipelineBuilder TenantResolverPipelineBuilder { get; set; } = new TenantResolverPipelineBuilder().Append<DefaultTenantResolver>();

    /// <summary>
    /// Gets or sets a value indicating whether a request, job or command whose tenant cannot be resolved is rejected instead of running as the default tenant.
    /// Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, <see cref="DefaultTenantResolver"/> is no longer consulted as a fallback, so the tenant must be resolved by another resolver.
    /// Resolving the default tenant explicitly (an empty tenant ID) still succeeds. The skip is by type: a custom resolver that always returns the default tenant is not skipped.
    /// </para>
    /// <para>
    /// HTTP requests with no resolved tenant get <c>400 Bad Request</c>. The tenant middleware answers every request, so health or static endpoints placed after <c>UseTenants()</c> also get <c>400</c>. Map them before it.
    /// </para>
    /// <para>
    /// A command with no tenant ID is rejected with an <see cref="InvalidOperationException"/> unless it is sent from inside a tenant scope. A custom scheduler that sends commands from outside a tenant scope
    /// gets that exception, which is logged, and the work is dropped.
    /// </para>
    /// </remarks>
    public bool RejectUnresolvedTenants { get; set; }
}
