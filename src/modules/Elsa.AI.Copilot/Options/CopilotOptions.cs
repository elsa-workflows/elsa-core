namespace Elsa.AI.Copilot.Options;

public class CopilotOptions
{
    public string CliPath { get; set; } = "copilot";
    public string? Model { get; set; }
    public string? ProviderName { get; set; } = "copilot";
}
