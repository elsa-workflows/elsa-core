using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Builder;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class DiscoverLoginMethods(IExternalAuthenticationBroker broker, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest<DiscoverLoginMethodsResponse>
{
    public override void Configure()
    {
        Get("/external-authentication/login-methods");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(ExternalAuthenticationRateLimitPolicyNames.Discovery));
    }

    public override async Task<DiscoverLoginMethodsResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        var clientId = Query<string>("clientId", true) ?? Query<string>("client_id", true) ?? string.Empty;
        try
        {
            var methods = await broker.DiscoverAsync(tenantAccessor.TenantId, clientId, cancellationToken);
            HttpContext.Response.Headers.CacheControl = "no-store";
            return new(methods, methods.SingleOrDefault(x => x.IsPreferred)?.Key);
        }
        catch (InvalidOperationException)
        {
            HttpContext.Response.StatusCode = 400;
            return new([], null);
        }
    }
}

/// <summary>The preferred method is visual metadata only; clients must always render an explicit chooser.</summary>
internal sealed record DiscoverLoginMethodsResponse(IReadOnlyCollection<LoginMethod> Methods, string? PreferredMethodKey);
