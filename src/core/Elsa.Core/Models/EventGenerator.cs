using Elsa.Contracts;

namespace Elsa.Models;

public abstract class EventGenerator : Trigger, IEventGenerator
{
    protected EventGenerator()
    {
    }

    protected EventGenerator(string triggerType) : base(triggerType)
    {
    }
}