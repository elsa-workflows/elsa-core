namespace Elsa.Common.Multitenancy;

public record TenantDeletedEventArgs(Tenant Tenant, TenantScope TenantScope, CancellationToken CancellationToken) : TenantEventArgs(Tenant, TenantScope, CancellationToken);