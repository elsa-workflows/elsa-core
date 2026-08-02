using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class InitiateLocal(IExternalAuthenticationBroker broker, ITenantAccessor tenantAccessor) : ElsaEndpoint<InitiateLocalRequest>
{
    public override void Configure()
    {
        Post("/external-authentication/local/authorize");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(ExternalAuthenticationRateLimitPolicyNames.LocalInitiation));
    }

    public override async Task HandleAsync(InitiateLocalRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out var redirectUri))
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest), cancellationToken);
            return;
        }

        var result = await broker.InitiateLocalAsync(new LocalBrokerAuthorizationRequest(
            request.ClientId ?? string.Empty, redirectUri, request.ResponseType ?? string.Empty, request.CodeChallenge ?? string.Empty,
            request.CodeChallengeMethod ?? string.Empty, request.ReturnPath ?? string.Empty, request.Username ?? string.Empty,
            request.Password ?? string.Empty, request.State), tenantAccessor.TenantId, cancellationToken);
        if (result.Error is { } error)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        await Send.OkAsync(new InitiateLocalResponse(result.RedirectUri!.ToString()), cancellationToken);
    }
}

internal sealed class InitiateLocalRequest
{
    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? ResponseType { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? ReturnPath { get; set; }
    public string? State { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

internal sealed record InitiateLocalResponse(string RedirectUri);
