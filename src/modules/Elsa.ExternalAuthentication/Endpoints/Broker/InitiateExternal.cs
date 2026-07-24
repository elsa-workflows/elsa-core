using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal sealed class InitiateExternal(IExternalAuthenticationBroker broker, ITenantAccessor tenantAccessor) : ElsaEndpoint<InitiateExternalRequest>
{
    public override void Configure()
    {
        Get("/external-authentication/authorize/{connectionKey}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(ExternalAuthenticationRateLimitPolicyNames.ExternalInitiation));
    }

    public override async Task HandleAsync(InitiateExternalRequest request, CancellationToken cancellationToken)
    {
        var clientId = Query<string>("client_id", false) ?? request.ClientId;
        var redirectUriValue = Query<string>("redirect_uri", false) ?? request.RedirectUri;
        var responseType = Query<string>("response_type", false) ?? request.ResponseType;
        var codeChallenge = Query<string>("code_challenge", false) ?? request.CodeChallenge;
        var codeChallengeMethod = Query<string>("code_challenge_method", false) ?? request.CodeChallengeMethod;
        var returnPath = Query<string>("return_path", false) ?? request.ReturnPath;
        if (!Uri.TryCreate(redirectUriValue, UriKind.Absolute, out var redirectUri))
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest), cancellationToken);
            return;
        }

        var result = await broker.InitiateExternalAsync(new BrokerAuthorizationRequest(
            clientId ?? string.Empty, redirectUri, responseType ?? string.Empty, codeChallenge ?? string.Empty,
            codeChallengeMethod ?? string.Empty, returnPath ?? string.Empty, request.ConnectionKey ?? string.Empty, request.State), tenantAccessor.TenantId, cancellationToken);
        if (result.Error is { } error)
        {
            await BrokerEndpointSupport.SendErrorAsync(Send, error, cancellationToken);
            return;
        }

        await Send.RedirectAsync(result.NavigationUri!.ToString(), false, true);
    }
}

internal sealed class InitiateExternalRequest
{
    public string? ConnectionKey { get; set; }
    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? ResponseType { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? ReturnPath { get; set; }
    public string? State { get; set; }
}
