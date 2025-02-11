namespace Elsa.Tenants.AspNetCore.Options;

public class MultitenancyHttpOptions
{
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";
}