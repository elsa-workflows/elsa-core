using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Components;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer;

/// <summary>Adds the Server broker sign-out entry point to the Studio app bar.</summary>
public sealed class Feature(IAppBarService appBarService) : FeatureBase
{
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<BrokerLoginState>();
        return base.InitializeAsync(cancellationToken);
    }
}
