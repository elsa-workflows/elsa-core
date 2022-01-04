using Elsa.Runtime.Stimuli;

namespace Elsa.Runtime.Models;

public static class Stimuli
{
    public static StandardStimulus Standard(string activityTypeName, string? hash = default, IDictionary<string, object?>? data = default) => new(activityTypeName, hash, data);
}