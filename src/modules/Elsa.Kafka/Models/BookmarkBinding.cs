using Elsa.Kafka.Stimuli;

namespace Elsa.Kafka;

public record BookmarkBinding(string WorkflowInstanceId, string BookmarkId, MessageReceivedStimulus Stimulus);