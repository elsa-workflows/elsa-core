using System.Text.Json;

namespace Elsa.Platform.Integration.Services;

public interface IShellConfigurationOverlayStore
{
    Task<bool> ConfigureFeaturesAsync(
        string shellId,
        IReadOnlyDictionary<string, JsonElement>? enabledFeatures,
        IReadOnlyList<string>? disabledFeatures,
        CancellationToken cancellationToken = default);

    Task<bool> ConfigureSettingsAsync(
        string shellId,
        JsonElement settings,
        CancellationToken cancellationToken = default);
}
