namespace Elsa.AI.Host.Options;

public class AiHostOptions
{
    public TimeSpan ConversationRetention { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan ReconnectGrace { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxToolResultBytes { get; set; } = 64 * 1024;
    public int MaxResolvedContextBytes { get; set; } = 128 * 1024;
    public ICollection<AiProviderOptions> Providers { get; set; } = [];
}

public class AiProviderOptions
{
    public string Name { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string? Model { get; set; }
    public string? ApiKeySecretName { get; set; }
    public string? Endpoint { get; set; }
    public bool Enabled { get; set; } = true;
}
