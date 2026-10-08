using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Components;

namespace Elsa.Studio.Login;

/// <summary>
/// Represents the login feature module that provides authentication functionality for the Elsa Studio application.
/// </summary>
public class LoginFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc/>
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<LoginState>();
        return base.InitializeAsync(cancellationToken);
    }
}