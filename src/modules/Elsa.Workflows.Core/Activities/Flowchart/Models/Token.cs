namespace Elsa.Workflows.Activities.Flowchart.Models;

internal class Token(string fromActivityId, string? fromActivityName, string toActivityId, string? toActivityName)
{
    public static Token Create(IActivity from, IActivity to) => new(from.Id, from.Name, to.Id, to.Name);

    public string FromActivityId { get; } = fromActivityId;
    public string? FromActivityName { get; } = fromActivityName;
    public string ToActivityId { get; } = toActivityId;
    public string? ToActivityName { get; } = toActivityName;
    public bool Scheduled { get; set; }
    public bool Consumed { get; private set; }

    public Token Schedule()
    {
        Scheduled = true;
        return this;
    }

    public Token Consume()
    {
        Consumed = true;
        return this;
    }
}