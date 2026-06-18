using System.IO.Compression;
using System.Security.Cryptography;
using CShells.Lifecycle;
using Elsa.Platform.Integration.Models;
using Elsa.Platform.Integration.Steps;
using Loom;

namespace Elsa.Platform.Integration.Services;

public class ElsaLoomRecipeArtifactApplier(
    IServiceProvider serviceProvider,
    IShellRegistry shellRegistry) : IPlatformRecipeArtifactApplier
{
    private static readonly JsonRecipeSerializer Serializer = new();

    public async Task<PlatformRecipeArtifactApplyResult> ApplyAsync(
        PlatformRuntimeCommand command,
        PlatformArtifactItem artifact,
        Stream artifactZip,
        CancellationToken cancellationToken = default)
    {
        var observedDigest = await ComputeDigestAsync(artifactZip, cancellationToken);
        if (!DigestEquals(observedDigest, artifact.ContentDigest))
        {
            return Rejected(
                observedDigest,
                "elsa-platform.artifact-digest-mismatch",
                "Downloaded recipe artifact digest did not match the Platform command digest.");
        }

        var textEntries = await ReadTextEntriesAsync(artifactZip, cancellationToken);
        var recipeJson = FindRecipeJson(textEntries);
        if (recipeJson is null)
            return Rejected(observedDigest, "elsa-platform.recipe-payload-missing", "Loom recipe artifact ZIP did not contain a recipe JSON payload.");

        Recipe recipe;
        try
        {
            recipe = Serializer.Deserialize(recipeJson);
        }
        catch (RecipeSerializationException ex)
        {
            return Rejected(observedDigest, "elsa-platform.recipe-payload-invalid", ex.Message);
        }

        var reloadTracker = new PlatformShellReloadTracker();
        var recipeServices = new PlatformRecipeServiceProvider(
            serviceProvider,
            new Dictionary<Type, object>
            {
                [typeof(PlatformRecipeArtifact)] = new PlatformRecipeArtifact(textEntries),
                [typeof(PlatformShellReloadTracker)] = reloadTracker
            });

        var engine = RecipeEngine.Create()
            .RegisterStep<VerifyCapabilitiesStep>()
            .RegisterStep<ImportWorkflowDefinitionStep>()
            .RegisterStep<ConfigureFeaturesStep>()
            .RegisterStep<ConfigureSettingsStep>();

        var runResult = await engine.RunAsync(recipe, new RecipeRunOptions
        {
            Services = recipeServices
        }, cancellationToken);

        if (!runResult.Succeeded)
        {
            var status = runResult.Status == RecipeRunStatus.ValidationFailed
                ? PlatformArtifactStatus.Rejected
                : PlatformArtifactStatus.Failed;
            return new PlatformRecipeArtifactApplyResult(
                status,
                observedDigest,
                RuntimeReference(recipe),
                ToPlatformDiagnostics(runResult.Diagnostics, runResult.Error));
        }

        var reloadFailure = await ReloadShellsAsync(reloadTracker, cancellationToken);
        if (reloadFailure is not null)
        {
            return new PlatformRecipeArtifactApplyResult(
                PlatformArtifactStatus.Failed,
                observedDigest,
                RuntimeReference(recipe),
                [reloadFailure]);
        }

        var diagnostics = ToPlatformDiagnostics(runResult.Diagnostics, null);
        if (diagnostics.Count == 0)
            diagnostics = [PlatformDiagnosticSanitizer.Info("elsa-platform.recipe-applied", "Loom recipe artifact was applied.")];

        return new PlatformRecipeArtifactApplyResult(
            PlatformArtifactStatus.Applied,
            observedDigest,
            RuntimeReference(recipe),
            diagnostics);
    }

    private async Task<PlatformDiagnostic?> ReloadShellsAsync(
        PlatformShellReloadTracker reloadTracker,
        CancellationToken cancellationToken)
    {
        foreach (var shellId in reloadTracker.ShellIds)
        {
            var result = await shellRegistry.ReloadAsync(shellId, cancellationToken);
            if (result.Error is not null)
            {
                return PlatformDiagnosticSanitizer.Error(
                    "elsa-platform.shell-reload-failed",
                    $"Shell '{shellId}' reload failed: {result.Error.Message}");
            }
        }

        return null;
    }

    private static async Task<IReadOnlyDictionary<string, string>> ReadTextEntriesAsync(
        Stream artifactZip,
        CancellationToken cancellationToken)
    {
        artifactZip.Position = 0;
        using var archive = new ZipArchive(artifactZip, ZipArchiveMode.Read, leaveOpen: true);
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || !entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            await using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            entries[NormalizePath(entry.FullName)] = await reader.ReadToEndAsync(cancellationToken);
        }

        return entries;
    }

    private static string? FindRecipeJson(IReadOnlyDictionary<string, string> textEntries)
    {
        var recipePath = textEntries.Keys
            .Where(x => x.StartsWith("payload/recipes/", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (recipePath is not null)
            return textEntries[recipePath];

        foreach (var candidate in new[] { "recipe.json", "loom.recipe.json" })
        {
            if (textEntries.TryGetValue(candidate, out var recipeJson))
                return recipeJson;
        }

        return null;
    }

    private static async Task<PlatformArtifactDigest> ComputeDigestAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        stream.Position = 0;
        return new PlatformArtifactDigest("sha256", Convert.ToHexString(hash).ToLowerInvariant());
    }

    private static IReadOnlyList<PlatformDiagnostic> ToPlatformDiagnostics(
        IReadOnlyList<RecipeDiagnostic> diagnostics,
        string? fallbackError)
    {
        var platformDiagnostics = diagnostics.Select(ToPlatformDiagnostic).ToList();
        if (platformDiagnostics.Count == 0 && !string.IsNullOrWhiteSpace(fallbackError))
            platformDiagnostics.Add(PlatformDiagnosticSanitizer.Error("elsa-platform.recipe-failed", fallbackError));

        return platformDiagnostics;
    }

    private static PlatformDiagnostic ToPlatformDiagnostic(RecipeDiagnostic diagnostic)
    {
        var message = diagnostic.ExceptionSummary is null
            ? diagnostic.Message
            : $"{diagnostic.Message} {diagnostic.ExceptionSummary}";
        return diagnostic.Severity switch
        {
            DiagnosticSeverity.Information => PlatformDiagnosticSanitizer.Info(diagnostic.Code, message),
            DiagnosticSeverity.Warning => PlatformDiagnosticSanitizer.Warning(diagnostic.Code, message),
            _ => PlatformDiagnosticSanitizer.Error(diagnostic.Code, message)
        };
    }

    private static bool DigestEquals(PlatformArtifactDigest left, PlatformArtifactDigest right) =>
        left.Algorithm.Equals(right.Algorithm, StringComparison.OrdinalIgnoreCase)
        && left.Value.Equals(right.Value, StringComparison.OrdinalIgnoreCase);

    private static PlatformRecipeArtifactApplyResult Rejected(
        PlatformArtifactDigest observedDigest,
        string code,
        string message) =>
        new(PlatformArtifactStatus.Rejected, observedDigest, null, [PlatformDiagnosticSanitizer.Error(code, message)]);

    private static string RuntimeReference(Recipe recipe) =>
        recipe.Version is null
            ? $"elsa://loom-recipes/{Uri.EscapeDataString(recipe.Name)}"
            : $"elsa://loom-recipes/{Uri.EscapeDataString(recipe.Name)}@{Uri.EscapeDataString(recipe.Version)}";

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
