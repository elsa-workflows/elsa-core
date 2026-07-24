using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.Endpoints.Previews;

internal sealed class InitiatePreview(PreviewSignInService previews, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/external-authentication/connections/{connectionId}/preview");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsPreview);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (!Endpoints.Connections.ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status428PreconditionRequired;
            return;
        }
        var result = await previews.InitiateAsync(Route<string>("connectionId")!, revision, tenantAccessor.TenantId, User, cancellationToken);
        switch (result)
        {
            case PreviewInitiationResult.Started(var handle, var expiresAt):
                await HttpContext.Response.WriteAsJsonAsync(new PreviewInitiationResponse($"/external-authentication/previews/{Uri.EscapeDataString(handle)}/authorize", expiresAt), cancellationToken);
                return;
            case PreviewInitiationResult.PreconditionFailed(var currentRevision):
                HttpContext.Response.StatusCode = StatusCodes.Status412PreconditionFailed;
                await HttpContext.Response.WriteAsJsonAsync(new { error = "precondition_failed", currentRevision }, cancellationToken);
                return;
            case PreviewInitiationResult.NotFound:
                HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            default:
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
        }
    }
}

internal sealed class AuthorizePreview(PreviewSignInService previews, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/external-authentication/previews/{previewHandle}/authorize");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsPreview);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await previews.AuthorizeAsync(Route<string>("previewHandle")!, tenantAccessor.TenantId, User, cancellationToken);
        if (result is PreviewAuthorizeResult.Redirect(var navigationUri))
        {
            HttpContext.Response.Redirect(navigationUri.ToString());
            return;
        }
        HttpContext.Response.StatusCode = StatusCodes.Status410Gone;
    }
}

internal sealed class CompletePreview(PreviewSignInService previews) : ElsaEndpointWithoutRequest
{
    public override void Configure() => Get("/external-authentication/previews/callback/{connectionId}");

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var parameters = HttpContext.Request.Query.ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.Value.Where(value => value is not null).Select(value => value!).ToArray(), StringComparer.Ordinal);
        var result = await previews.CompleteAsync(Route<string>("connectionId")!, HttpContext.Request.Query["state"].ToString(), parameters, cancellationToken);
        HttpContext.Response.StatusCode = result is PreviewCallbackResult.Completed ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        await HttpContext.Response.WriteAsJsonAsync(new { completed = result is PreviewCallbackResult.Completed }, cancellationToken);
    }
}

internal sealed class GetPreviewResult(PreviewSignInService previews, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/external-authentication/previews/{previewHandle}");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsPreview);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await previews.TakeResultAsync(Route<string>("previewHandle")!, tenantAccessor.TenantId, User, cancellationToken);
        if (result is TakeResult<PreviewResult>.Taken { Value: var preview })
        {
            await HttpContext.Response.WriteAsJsonAsync(PreviewResultDocument.From(preview), cancellationToken);
            return;
        }
        HttpContext.Response.StatusCode = result is TakeResult<PreviewResult>.Expired or TakeResult<PreviewResult>.AlreadyConsumed ? StatusCodes.Status410Gone : StatusCodes.Status404NotFound;
    }
}

internal sealed record PreviewInitiationResponse(string NavigationUrl, DateTimeOffset ExpiresAt);
internal sealed record PreviewResultDocument(string ConnectionId, string MaterialRevision, string Issuer, string MaskedSubject, IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectedClaims, string PolicyDecision, IReadOnlyCollection<PermissionGrant> PermissionProjection, IReadOnlyCollection<string> Warnings)
{
    public static PreviewResultDocument From(PreviewResult value) => new(value.ConnectionId, value.MaterialRevision, value.Issuer, value.MaskedSubject, value.ProjectedClaims, value.PolicyDecision, value.PermissionProjection, value.Warnings);
}
