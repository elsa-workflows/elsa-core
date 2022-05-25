using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime.Models;

public static class Stimulus
{
    public static StandardStimulus Standard(string activityTypeName, string? hash = default, IDictionary<string, object>? input = default, string? correlationId = default) => new(activityTypeName, hash, input, correlationId);
    public static StandardStimulus Standard(string activityTypeName, string? hash, object input, string? correlationId = default) => new(activityTypeName, hash, input.ToDictionary(), correlationId);
    public static StandardStimulus Standard(string activityTypeName, object input, string? correlationId = default) => new(activityTypeName, default, input.ToDictionary(), correlationId);
}