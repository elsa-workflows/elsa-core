using System.Security.Claims;
using Elsa.AI.Host.Options;
using Elsa.Authorization;
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
        var tenantAccessor = context?.RequestServices?.GetService<ITenantAccessor>();
        if (tenantAccessor != null)
            return tenantAccessor.TenantId;

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

    public static string? GetAuthorizedAgent(string? requestedAgent, AIHostOptions options, ClaimsPrincipal? user)
    {
        if (string.IsNullOrWhiteSpace(requestedAgent))
            return null;

        var agent = options.Agents.FirstOrDefault(x => string.Equals(x.Name, requestedAgent, StringComparison.OrdinalIgnoreCase));
        if (agent == null || !HasRequiredPermissions(agent.Permissions, user))
            return null;

        return agent.Name;
    }

    // Routed through the shared evaluator so a wildcard grant such as ai/*:execute reaches an agent's declared
    // permissions, and so the comparison is ordinal like every other site in the model. The previous
    // case-insensitive exact-set containment did neither: it admitted casing the rest of the model rejects,
    // while refusing the wildcards the rest of the model honours.
    private static bool HasRequiredPermissions(ICollection<string> requiredPermissions, ClaimsPrincipal? user)
    {
        if (requiredPermissions.Count == 0)
            return true;
        if (user is null)
            return false;

        return requiredPermissions.All(x => Permission.TryParse(x, out var required) && PermissionEvaluator.Shared.HasPermission(user, required));
    }
}
