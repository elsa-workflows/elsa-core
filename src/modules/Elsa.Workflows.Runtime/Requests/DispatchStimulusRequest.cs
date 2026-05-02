namespace Elsa.Workflows.Runtime.Requests;

public class DispatchStimulusRequest
{
    public string? ActivityTypeName { get; set; }
    public object? Stimulus { get; set; }
    public string? StimulusHash { get; set; }
    public StimulusMetadata? Metadata { get; set; }

    /// <summary>
    /// Name of the ingress source that initiated this dispatch. Carried through to the execution cycle registry for
    /// drain-time accounting and the FR-018 inconsistency detection. Null when no attribution is available.
    /// </summary>
    public string? IngressSourceName { get; set; }
}