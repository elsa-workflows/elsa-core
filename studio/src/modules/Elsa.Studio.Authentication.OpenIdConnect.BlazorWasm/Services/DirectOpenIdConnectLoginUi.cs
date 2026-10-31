using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;

/// <summary>Contributes the legacy direct OIDC navigation to the shared login shell.</summary>
public sealed class DirectOpenIdConnectLoginMethodCatalog : ILoginMethodCatalog
{
    public ValueTask<LoginMethodCatalogResult> ListAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new LoginMethodCatalogResult(
        [
            new LoginMethodDescriptor(
                "direct-openid-connect",
                "direct-openid-connect",
                "direct-openid-connect",
                "Single sign-on",
                null,
                0,
                true,
                "/authentication/login")
        ],
        "direct-openid-connect"));
}

public sealed class DirectOpenIdConnectLoginMethodComponentProvider : ILoginMethodComponentProvider
{
    public string Kind => "direct-openid-connect";
    public Type ComponentType => typeof(DirectOpenIdConnectLoginMethod);
}
