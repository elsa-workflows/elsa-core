using System.Text.Json;
using Elsa.Platform.Integration.Services;
using Loom;

namespace Elsa.Platform.Integration.Steps;

[Step("elsa.configure-settings")]
public sealed class ConfigureSettingsStep(
    IShellConfigurationOverlayStore overlayStore,
    PlatformShellReloadTracker reloadTracker) : IStep, IValidatingStep
{
    public string ShellId { get; init; } = "Default";

    public JsonElement Settings { get; init; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        List<RecipeDiagnostic> diagnostics = [];
        if (string.IsNullOrWhiteSpace(ShellId))
            diagnostics.Add(context.Error("ELSA_PLATFORM_SHELL_ID_REQUIRED", "Shell ID is required.", context.Target("input.shellId")));

        if (Settings.ValueKind != JsonValueKind.Object)
            diagnostics.Add(context.Error("ELSA_PLATFORM_SETTINGS_REQUIRED", "Settings must be a JSON object.", context.Target("input.settings")));

        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(diagnostics);
    }

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        var changed = await overlayStore.ConfigureSettingsAsync(ShellId, Settings, cancellationToken);
        if (changed)
            reloadTracker.MarkForReload(ShellId);
    }
}
