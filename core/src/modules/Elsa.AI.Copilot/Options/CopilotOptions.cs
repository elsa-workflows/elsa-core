namespace Elsa.AI.Copilot.Options;

public class CopilotOptions
{
    public string? RuntimePath { get; set; }
    public string? RuntimeUrl { get; set; }
    public string? ConnectionToken { get; set; }
    public ICollection<string> RuntimeArguments { get; set; } = [];
    public string? WorkingDirectory { get; set; }
    public string? BaseDirectory { get; set; }
    public string? GitHubToken { get; set; }
    public bool? UseLoggedInUser { get; set; }
    public bool EnableStreaming { get; set; } = true;
    public bool IncludeSubAgentStreamingEvents { get; set; } = true;
    public string? Model { get; set; }
    public string? ReasoningEffort { get; set; }
    public string? ProviderName { get; set; } = "copilot";
}
