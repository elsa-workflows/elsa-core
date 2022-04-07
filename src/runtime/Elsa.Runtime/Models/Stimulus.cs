using Elsa.Runtime.Stimuli;

namespace Elsa.Runtime.Models;

public static class Stimulus
{
    public static StandardStimulus Standard(string activityTypeName, string? hash = default, IDictionary<string, object>? input = default) => new(activityTypeName, hash, input);
    public static StandardStimulus Standard(string activityTypeName, string? hash, object input) => new(activityTypeName, hash, input.ToDictionary());
    public static StandardStimulus Standard(string activityTypeName, object input) => new(activityTypeName, default, input.ToDictionary());
}