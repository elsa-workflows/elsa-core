using System.Security.Claims;
using Elsa.AI.Host.Options;
using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Endpoints.AI;

internal static class AIHttpContextIdentity
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

    public static string? GetAuthorizedAgent(string? requestedAgent, AIHostOptions options, ICollection<string> userPermissions)
    {
        if (string.IsNullOrWhiteSpace(requestedAgent))
            return null;

        var agent = options.Agents.FirstOrDefault(x => string.Equals(x.Name, requestedAgent, StringComparison.OrdinalIgnoreCase));
        if (agent == null || !HasRequiredPermissions(agent.Permissions, userPermissions))
            return null;

        return agent.Name;
    }

    private static bool HasRequiredPermissions(ICollection<string> requiredPermissions, ICollection<string> userPermissions)
    {
        if (requiredPermissions.Count == 0)
            return true;

        var grantedPermissions = userPermissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return grantedPermissions.Contains(PermissionNames.All) || requiredPermissions.All(grantedPermissions.Contains);
    }
}
