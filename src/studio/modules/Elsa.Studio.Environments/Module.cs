using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;
using Elsa.Studio.Models;

namespace Elsa.Studio.Environments;

/// <summary>
/// Represents the environments feature module for managing server environment selection in the app bar.
/// </summary>
public class Feature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddElement(new AppBarElement<EnvironmentPicker>());
        return ValueTask.CompletedTask;
    }
}
