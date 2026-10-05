using Elsa.Studio.Abstractions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components;
using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm;

/// <summary>
/// Represents the OpenID Connect feature specific to Blazor WebAssembly authentication
/// within the Elsa Studio platform.
/// </summary>
/// <remarks>
/// This feature integrates OpenID Connect authentication capabilities into the
/// Blazor WebAssembly context, allowing for secure user authentication in a
/// distributed environment. It derives from the <see cref="FeatureBase"/> class,
/// which provides a framework for modules extending the Elsa Studio dashboard.
/// It also adds the user menu, including the sign-out entry point, to the Studio app bar.
/// </remarks>
[UsedImplicitly]
public class OpenIdConnectBlazorWasmFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<OpenIdConnectUserMenu>();
        return base.InitializeAsync(cancellationToken);
    }
}
