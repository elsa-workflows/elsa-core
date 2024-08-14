namespace Elsa.Agents;

public class ExecutionSettingsConfig
{
    public int? MaxTokens { get; set; }
    public double Temperature { get; set; }
    public double TopP { get; set; }
    public double PresencePenalty { get; set; }
    public double FrequencyPenalty { get; set; }
    public string? ResponseFormat { get; set; }
}