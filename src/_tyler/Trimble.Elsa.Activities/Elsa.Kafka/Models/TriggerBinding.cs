using Elsa.Kafka.Stimuli;
using Elsa.Workflows.Activities;

namespace Elsa.Kafka;

public record TriggerBinding(Workflow Workflow, string TriggerId, string TriggerActivityId, MessageReceivedStimulus Stimulus);