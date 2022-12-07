using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public abstract class EventGenerator : Trigger, IEventGenerator
{
    protected EventGenerator(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected EventGenerator(string triggerType, int version = 1, string? source = default, int? line = default) : base(triggerType, version, source, line)
    {
    }
}