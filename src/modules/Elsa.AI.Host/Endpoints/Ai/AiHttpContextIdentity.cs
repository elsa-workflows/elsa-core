using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Endpoints.Ai;

internal static class AiHttpContextIdentity
{
    private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

    public static string GetActorId(HttpContext? context) =>
        context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        context?.User.FindFirstValue("sub") ??
        context?.User.Identity?.Name ??
        "anonymous";

    public static string? GetTenantId(HttpContext? context)
    {
        var tenantId = context?.RequestServices?.GetService<ITenantAccessor>()?.TenantId;
        if (!string.IsNullOrWhiteSpace(tenantId))
            return tenantId;

        return context?.User.FindFirstValue(TenantIdClaimType) ??
               context?.User.FindFirstValue("tenant_id") ??
               context?.User.FindFirstValue("tenantId");
    }

    public static ICollection<string> GetPermissions(HttpContext? context) =>
        context?.User
            .FindAll(PermissionNames.ClaimType)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
}
