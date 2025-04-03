namespace Elsa.Workflows.Runtime.Requests;

public class DispatchStimulusRequest
{
    public string? ActivityTypeName { get; set; }
    public object? Stimulus { get; set; }
    public string? StimulusHash { get; set; }
    public StimulusMetadata? Metadata { get; set; }
}