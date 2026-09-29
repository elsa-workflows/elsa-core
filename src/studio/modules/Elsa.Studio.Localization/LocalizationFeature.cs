using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Components;

namespace Elsa.Studio.Localization;

/// <summary>
/// Represents the localization feature for the application, extending the base functionality of <see cref="FeatureBase"/>.
/// It provides a mechanism to initialize the language picker component in the application bar.
/// </summary>
public class LocalizationFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<LanguagePicker>();

        return ValueTask.CompletedTask;
    }
}
