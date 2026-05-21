using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.AI.Host.Extensions;
using Elsa.AI.Host.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.ShellFeatures;

[ShellFeature(
    "Ai",
    DisplayName = "AI Host",
    Description = "Hosts Weaver AI orchestration, governed tools, proposals, and audit services",
    DependsOn = ["ElsaFastEndpoints"])]
[UsedImplicitly]
public class AiFeature : IFastEndpointsShellFeature
{
    private static readonly AiHostOptions DefaultOptions = new();

    public bool StreamingEnabled { get; set; } = DefaultOptions.StreamingEnabled;
    public bool ConversationPersistenceEnabled { get; set; } = DefaultOptions.ConversationPersistenceEnabled;
    public bool ProposalReviewEnabled { get; set; } = DefaultOptions.ProposalReviewEnabled;
    public TimeSpan ConversationRetention { get; set; } = DefaultOptions.ConversationRetention;
    public TimeSpan ReconnectGrace { get; set; } = DefaultOptions.ReconnectGrace;
    public int MaxToolResultBytes { get; set; } = DefaultOptions.MaxToolResultBytes;
    public int MaxResolvedContextBytes { get; set; } = DefaultOptions.MaxResolvedContextBytes;
    public string? DefaultProviderName { get; set; } = DefaultOptions.DefaultProviderName;
    public ICollection<AiProviderOptions> Providers { get; set; } = [];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAiHostServices(ConfigureOptions);
    }

    private void ConfigureOptions(AiHostOptions options)
    {
        options.StreamingEnabled = StreamingEnabled;
        options.ConversationPersistenceEnabled = ConversationPersistenceEnabled;
        options.ProposalReviewEnabled = ProposalReviewEnabled;
        options.ConversationRetention = ConversationRetention;
        options.ReconnectGrace = ReconnectGrace;
        options.MaxToolResultBytes = MaxToolResultBytes;
        options.MaxResolvedContextBytes = MaxResolvedContextBytes;
        options.DefaultProviderName = DefaultProviderName;
        options.Providers = [..Providers];
    }
}
