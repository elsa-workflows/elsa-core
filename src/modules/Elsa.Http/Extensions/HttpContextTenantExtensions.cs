using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="HttpContext"/> class.
/// </summary>
public static class HttpContextTenantExtensions
{
    /// <summary>
    /// Sets the tenant ID for the current HTTP context.
    /// </summary>
    public static void SetTenantId(this HttpContext httpContext, string? tenantId)
    {
        httpContext.Items["TenantId"] = tenantId;
    }

    /// <summary>
    /// Gets the tenant ID for the current HTTP context.
    /// </summary>
    public static string? GetTenantId(this HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue("TenantId", out var tenantId) ? tenantId as string : null;
    }
}