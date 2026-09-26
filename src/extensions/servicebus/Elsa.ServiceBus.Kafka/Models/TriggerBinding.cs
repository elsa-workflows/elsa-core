using Elsa.ServiceBus.Kafka.Stimuli;
using Elsa.Workflows.Activities;

namespace Elsa.ServiceBus.Kafka;

public record TriggerBinding(Workflow Workflow, string TriggerId, string TriggerActivityId, MessageReceivedStimulus Stimulus, string? TenantId);
