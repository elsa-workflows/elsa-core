namespace Elsa.AI.Host.Options;

public class AIHostOptions
{
    public bool StreamingEnabled { get; set; } = true;
    public bool ConversationPersistenceEnabled { get; set; } = true;
    public bool ProposalReviewEnabled { get; set; } = true;
    public TimeSpan ConversationRetention { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan ReconnectGrace { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxToolResultBytes { get; set; } = 64 * 1024;
    public int MaxResolvedContextBytes { get; set; } = 128 * 1024;
    public string? DefaultProviderName { get; set; }
    public ICollection<AIProviderOptions> Providers { get; set; } = [];
    public ICollection<string> SupportedAttachmentKinds { get; set; } = ["WorkflowDefinition", "WorkflowInstance"];
    public ICollection<AIAgentOptions> Agents { get; set; } = [new() { Name = "workflow-author", DisplayName = "Workflow author", Description = "Creates safe workflow proposals" }];
}

public class AIProviderOptions
{
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "";
    public string? Model { get; set; }
    public string? ApiKeySecretName { get; set; }
    public string? Endpoint { get; set; }
    public bool Enabled { get; set; } = true;
}

public class AIAgentOptions
{
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? ProviderName { get; set; }
    public ICollection<string> Permissions { get; set; } = [];
}
