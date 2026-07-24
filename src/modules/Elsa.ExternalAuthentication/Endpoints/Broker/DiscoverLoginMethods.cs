using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

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
            return new DiscoverLoginMethodsResponse(methods, methods.SingleOrDefault(x => x.IsDefault)?.Key);
        }
        catch (InvalidOperationException)
        {
            HttpContext.Response.StatusCode = 400;
            return new DiscoverLoginMethodsResponse([], null);
        }
    }
}

internal sealed record DiscoverLoginMethodsResponse(IReadOnlyCollection<LoginMethod> Methods, string? AutomaticMethodKey);
