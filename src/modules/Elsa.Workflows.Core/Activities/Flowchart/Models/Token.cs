namespace Elsa.Workflows.Activities.Flowchart.Models;

internal class Token(string fromActivityId, string? fromActivityName, string? outcome, string toActivityId, string? toActivityName, bool consumed, bool scheduled)
{
    public static Token Create(IActivity from, IActivity to, string? outcome) => new(from.Id, from.Name, outcome, to.Id, to.Name, false, false);

    public string FromActivityId { get; } = fromActivityId;
    public string? FromActivityName { get; } = fromActivityName;
    public string? Outcome { get; } = outcome;
    public string ToActivityId { get; } = toActivityId;
    public string? ToActivityName { get; } = toActivityName;
    public bool Scheduled { get; private set; } = scheduled;
    public bool Consumed { get; private set; } = consumed;

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