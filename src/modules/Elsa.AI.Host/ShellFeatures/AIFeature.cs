using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.AI.Host.Options;
using Elsa.Extensions;
using Elsa.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.ShellFeatures;

[ShellFeature(
    "AI",
    DisplayName = "AI Host",
    Description = "Hosts Weaver AI orchestration, governed tools, proposals, and audit services",
    DependsOn = [typeof(ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public class AIFeature : IFastEndpointsShellFeature
{
    private static readonly AIHostOptions DefaultOptions = new();

    public bool StreamingEnabled { get; set; } = DefaultOptions.StreamingEnabled;
    public bool ConversationPersistenceEnabled { get; set; } = DefaultOptions.ConversationPersistenceEnabled;
    public bool ProposalReviewEnabled { get; set; } = DefaultOptions.ProposalReviewEnabled;
    public TimeSpan ConversationRetention { get; set; } = DefaultOptions.ConversationRetention;
    public TimeSpan ReconnectGrace { get; set; } = DefaultOptions.ReconnectGrace;
    public int MaxToolResultBytes { get; set; } = DefaultOptions.MaxToolResultBytes;
    public int MaxResolvedContextBytes { get; set; } = DefaultOptions.MaxResolvedContextBytes;
    public string? DefaultProviderName { get; set; } = DefaultOptions.DefaultProviderName;
    public ICollection<AIProviderOptions> Providers { get; set; } = [];
    public ICollection<AIAgentOptions> Agents { get; set; } = [..DefaultOptions.Agents];
    public ICollection<string> SupportedAttachmentKinds { get; set; } = [..DefaultOptions.SupportedAttachmentKinds];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAIHostServices(ConfigureOptions);
    }

    private void ConfigureOptions(AIHostOptions options)
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
        options.Agents = [..Agents];
        options.SupportedAttachmentKinds = [..SupportedAttachmentKinds];
    }
}
