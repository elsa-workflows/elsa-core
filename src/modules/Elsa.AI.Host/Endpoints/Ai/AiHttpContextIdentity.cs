using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Elsa.AI.Host.Endpoints.Ai;

internal static class AiHttpContextIdentity
{
    private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

    public static string GetActorId(HttpContext? context) =>
        context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        context?.User.FindFirstValue("sub") ??
        context?.User.Identity?.Name ??
        "anonymous";

    public static string? GetTenantId(HttpContext? context) =>
        context?.User.FindFirstValue(TenantIdClaimType) ??
        context?.User.FindFirstValue("tenant_id") ??
        context?.User.FindFirstValue("tenantId");
}
