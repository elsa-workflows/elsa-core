namespace Elsa.Workflows.Activities.Flowchart.Models;

internal class Token(string fromActivityId, string? fromActivityName, string toActivityId, string? toActivityName, bool consumed)
{
    public static Token Create(IActivity from, IActivity to) => new(from.Id, from.Name, to.Id, to.Name, false);

    public string FromActivityId { get; } = fromActivityId;
    public string? FromActivityName { get; } = fromActivityName;
    public string ToActivityId { get; } = toActivityId;
    public string? ToActivityName { get; } = toActivityName;
    public bool Consumed { get; private set; } = consumed;
        
    public Token Consume()
    {
        Consumed = true;
        return this;
    }
}