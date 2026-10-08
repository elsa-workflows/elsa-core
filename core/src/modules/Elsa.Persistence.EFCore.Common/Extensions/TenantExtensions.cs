using Elsa.Common.Multitenancy;

namespace Elsa.Persistence.EFCore.Extensions;

public static class TenantExtensions
{
    public static string? GetConnectionString(this Tenant tenant, string name)
    {
        return tenant.Configuration.GetSection("ConnectionStrings")[name];
    }
}