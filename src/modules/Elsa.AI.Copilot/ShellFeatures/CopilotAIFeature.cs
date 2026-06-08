using CShells.Features;
using Elsa.AI.Copilot.Options;
using Elsa.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Copilot.ShellFeatures;

[ManifestFeatureCategory("AI")]
[ShellFeature(
    "CopilotAI",
    DisplayName = "Copilot AI Provider",
    Description = "Registers the Copilot adapter for Weaver AI")]
[UsedImplicitly]
public class CopilotAIFeature : IShellFeature
{
    private static readonly CopilotOptions DefaultOptions = new();

    public string CliPath { get; set; } = DefaultOptions.CliPath;
    public string? RuntimePath { get; set; } = DefaultOptions.RuntimePath;
    public string? RuntimeUrl { get; set; } = DefaultOptions.RuntimeUrl;
    public string? WorkingDirectory { get; set; } = DefaultOptions.WorkingDirectory;
    public string? BaseDirectory { get; set; } = DefaultOptions.BaseDirectory;
    public string? GitHubToken { get; set; } = DefaultOptions.GitHubToken;
    public bool? UseLoggedInUser { get; set; } = DefaultOptions.UseLoggedInUser;
    public string? Model { get; set; } = DefaultOptions.Model;
    public string? ReasoningEffort { get; set; } = DefaultOptions.ReasoningEffort;
    public string? ProviderName { get; set; } = DefaultOptions.ProviderName;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCopilotAIProvider(ConfigureOptions);
    }

    private void ConfigureOptions(CopilotOptions options)
    {
        options.RuntimePath = RuntimePath ?? CliPath;
        options.RuntimeUrl = RuntimeUrl;
        options.WorkingDirectory = WorkingDirectory;
        options.BaseDirectory = BaseDirectory;
        options.GitHubToken = GitHubToken;
        options.UseLoggedInUser = UseLoggedInUser;
        options.Model = Model;
        options.ReasoningEffort = ReasoningEffort;
        options.ProviderName = ProviderName;
    }
}
