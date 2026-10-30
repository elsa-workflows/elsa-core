namespace Elsa.Workflows;

/// <summary>
/// Generates events on a workflow instance.
/// </summary>
public abstract class EventGenerator : Trigger, IEventGenerator
{
    /// <inheritdoc />
    protected EventGenerator(string? source = null, int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected EventGenerator(string triggerType, int version = 1, string? source = null, int? line = null) : base(triggerType, version, source, line)
    {
    }
}