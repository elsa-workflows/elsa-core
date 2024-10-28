using Elsa.Common.Multitenancy;

namespace Elsa.EntityFrameworkCore.Extensions;

public static class TenantExtensions
{
    public static string? GetConnectionString(this Tenant tenant, string name)
    {
        return tenant.Configuration.GetSection("ConnectionStrings")[name];
    }
}