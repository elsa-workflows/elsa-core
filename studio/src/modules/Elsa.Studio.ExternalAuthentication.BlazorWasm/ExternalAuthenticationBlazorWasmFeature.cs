using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Components;
using JetBrains.Annotations;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm;

/// <summary>Adds the WebAssembly broker sign-out entry point to the Studio app bar.</summary>
[UsedImplicitly]
public sealed class ExternalAuthenticationBlazorWasmFeature(IAppBarService appBarService) : FeatureBase
{
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<BrokerLoginState>();
        return base.InitializeAsync(cancellationToken);
    }
}
