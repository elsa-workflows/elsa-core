using Elsa.Features.Contracts;
using Elsa.Platform.Integration.Options;
using Loom;
using Microsoft.Extensions.Options;

namespace Elsa.Platform.Integration.Steps;

[Step("elsa.verify-capabilities")]
public sealed class VerifyCapabilitiesStep(
    IInstalledFeatureProvider installedFeatureProvider,
    IOptions<ElsaPlatformIntegrationOptions> options) : IStep, IValidatingStep
{
    public List<string>? Features { get; init; }

    public List<string>? Capabilities { get; init; }

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        List<RecipeDiagnostic> diagnostics = [];
        var installedFeatures = installedFeatureProvider.List().ToList();
        foreach (var feature in Features ?? [])
        {
            var exists = installedFeatures.Any(x =>
                string.Equals(x.Name, feature, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.FullName, feature, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                diagnostics.Add(context.Error("ELSA_PLATFORM_FEATURE_MISSING", $"Required feature '{feature}' is not installed.", context.Target("input.features")));
        }

        var runtimeCapabilities = options.Value.Capabilities;
        foreach (var capability in Capabilities ?? [])
        {
            if (!runtimeCapabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
                diagnostics.Add(context.Error("ELSA_PLATFORM_CAPABILITY_MISSING", $"Required runtime capability '{capability}' is not available.", context.Target("input.capabilities")));
        }

        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(diagnostics);
    }

    public ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
