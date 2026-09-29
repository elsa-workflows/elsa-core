using Elsa.ServiceBus.Kafka.Stimuli;

namespace Elsa.ServiceBus.Kafka;

public record BookmarkBinding(string WorkflowInstanceId, string? CorrelationId, string BookmarkId, MessageReceivedStimulus Stimulus, string? TenantId);