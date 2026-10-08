using Elsa.Studio.Components;
using Elsa.Studio.Host.CustomElements.Services;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Host.CustomElements.Components;

/// <summary>
/// Represents the backend component base.
/// </summary>
public abstract class BackendComponentBase : StudioComponentBase
{
    [Parameter] public string? RemoteEndpoint { get; set; }
    [Parameter] public string? ApiKey { get; set; }
    [Parameter] public string? AccessToken { get; set; }
    [Parameter] public string? TenantId { get; set; }
    [Parameter] public string? TenantIdHeaderName { get; set; }
    [Inject] private BackendService BackendService { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!string.IsNullOrWhiteSpace(RemoteEndpoint))
            BackendService.RemoteEndpoint = RemoteEndpoint;

        if (!string.IsNullOrWhiteSpace(ApiKey))
            BackendService.ApiKey = ApiKey;

        if (!string.IsNullOrWhiteSpace(AccessToken))
            BackendService.AccessToken = AccessToken;
        
        if(!string.IsNullOrWhiteSpace(TenantId))
            BackendService.TenantId = TenantId;
        
        if(!string.IsNullOrWhiteSpace(TenantIdHeaderName))
            BackendService.TenantIdHeaderName = TenantIdHeaderName;
    }
}