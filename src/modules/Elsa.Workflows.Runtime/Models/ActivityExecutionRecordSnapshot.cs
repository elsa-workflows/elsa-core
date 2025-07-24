namespace Elsa.Workflows.Runtime;

public class ActivityExecutionRecordSnapshot
{
    public string? SerializedActivityState { get; set; }
    public string? SerializedOutputs { get; set; }
    public string? SerializedProperties { get; set; }
    public string? SerializedPayload { get; set; }
    public string? SerializedMetadata { get; set; }
    public string? SerializedException { get; set; }
    public string? SerializedActivityStateCompressionAlgorithm { get; set; }
}