using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public abstract class EventGenerator : Trigger, IEventGenerator
{
    protected EventGenerator()
    {
    }

    protected EventGenerator(string triggerType) : base(triggerType)
    {
    }
}