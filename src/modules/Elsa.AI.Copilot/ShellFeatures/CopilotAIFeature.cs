using CShells.Features;
using Elsa.AI.Copilot.Options;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Copilot.ShellFeatures;

[ShellFeature(
    "CopilotAI",
    DisplayName = "Copilot AI Provider",
    Description = "Registers the Copilot adapter for Weaver AI")]
[UsedImplicitly]
public class CopilotAIFeature : IShellFeature
{
    private static readonly CopilotOptions DefaultOptions = new();

    public string CliPath { get; set; } = DefaultOptions.CliPath;
    public string? Model { get; set; } = DefaultOptions.Model;
    public string? ProviderName { get; set; } = DefaultOptions.ProviderName;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCopilotAIProvider(ConfigureOptions);
    }

    private void ConfigureOptions(CopilotOptions options)
    {
        options.CliPath = CliPath;
        options.Model = Model;
        options.ProviderName = ProviderName;
    }
}
