using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Authorization;

/// <summary>
/// Resolves the <see cref="IPermissionEvaluator"/> for a request.
/// </summary>
public static class HttpContextPermissionExtensions
{
    /// <summary>
    /// Returns the evaluator registered for the request, or <see cref="PermissionEvaluator.Shared"/> when the host has
    /// registered none. Hosts that wire FastEndpoints themselves may skip the authorization registration, and the
    /// evaluator is stateless, so falling back to the shared instance is safe.
    /// </summary>
    public static IPermissionEvaluator GetPermissionEvaluator(this HttpContext context) =>
        context.RequestServices.GetService<IPermissionEvaluator>() ?? PermissionEvaluator.Shared;
}
