using System.Text.Json;
using Elsa.Platform.Integration.Services;
using Loom;

namespace Elsa.Platform.Integration.Steps;

[Step("elsa.configure-features")]
public sealed class ConfigureFeaturesStep(
    IShellConfigurationOverlayStore overlayStore,
    PlatformShellReloadTracker reloadTracker) : IStep, IValidatingStep
{
    public string ShellId { get; init; } = "Default";

    public Dictionary<string, JsonElement>? Enable { get; init; }

    public List<string>? Disable { get; init; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        List<RecipeDiagnostic> diagnostics = [];
        if (string.IsNullOrWhiteSpace(ShellId))
            diagnostics.Add(context.Error("ELSA_PLATFORM_SHELL_ID_REQUIRED", "Shell ID is required.", context.Target("input.shellId")));

        if ((Enable is null || Enable.Count == 0) && (Disable is null || Disable.Count == 0))
            diagnostics.Add(context.Error("ELSA_PLATFORM_FEATURE_CHANGE_REQUIRED", "At least one feature enable or disable entry is required.", context.Target("input")));

        foreach (var feature in Enable ?? [])
        {
            if (feature.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null and not JsonValueKind.Undefined)
                diagnostics.Add(context.Error("ELSA_PLATFORM_FEATURE_CONFIG_INVALID", $"Feature '{feature.Key}' configuration must be a JSON object.", context.Target($"input.enable.{feature.Key}")));
        }

        var duplicate = Enable?.Keys.FirstOrDefault(feature => Disable?.Contains(feature, StringComparer.OrdinalIgnoreCase) == true);
        if (duplicate is not null)
            diagnostics.Add(context.Error("ELSA_PLATFORM_FEATURE_CHANGE_CONFLICT", $"Feature '{duplicate}' cannot be enabled and disabled in the same step.", context.Target("input")));

        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(diagnostics);
    }

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        var changed = await overlayStore.ConfigureFeaturesAsync(ShellId, Enable, Disable, cancellationToken);
        if (changed)
            reloadTracker.MarkForReload(ShellId);
    }
}
