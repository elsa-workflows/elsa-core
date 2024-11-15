using Elsa.Kafka.Stimuli;

namespace Elsa.Kafka;

public record BookmarkBinding(string WorkflowInstanceId, string? CorrelationId, string BookmarkId, MessageReceivedStimulus Stimulus);