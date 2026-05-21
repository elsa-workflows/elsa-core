using CShells.Features;
using Elsa.AI.Copilot.Extensions;
using Elsa.AI.Copilot.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Copilot.ShellFeatures;

[ShellFeature(
    "CopilotAi",
    DisplayName = "Copilot AI Provider",
    Description = "Registers the Copilot adapter for Weaver AI")]
[UsedImplicitly]
public class CopilotAiFeature : IShellFeature
{
    private static readonly CopilotOptions DefaultOptions = new();

    public string CliPath { get; set; } = DefaultOptions.CliPath;
    public string? Model { get; set; } = DefaultOptions.Model;
    public string? ProviderName { get; set; } = DefaultOptions.ProviderName;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCopilotAiProvider(ConfigureOptions);
    }

    private void ConfigureOptions(CopilotOptions options)
    {
        options.CliPath = CliPath;
        options.Model = Model;
        options.ProviderName = ProviderName;
    }
}
