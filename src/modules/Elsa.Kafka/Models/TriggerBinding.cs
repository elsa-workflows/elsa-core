using Elsa.Kafka.Stimuli;
using Elsa.Workflows.Models;

namespace Elsa.Kafka;

public record TriggerBinding(string TriggerId, string TriggerActivityId, MessageReceivedStimulus Stimulus);