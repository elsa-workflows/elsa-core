using Hangfire.States;

namespace Elsa.Hangfire.States;

public class PendingState : IState
{
    public static readonly string StateName = "Pending";

    public Dictionary<string, string> SerializeData()
    {
        return new();
    }

    public string Name => StateName;
    public string Reason => "Pending";
    public bool IsFinal => false;
    public bool IgnoreJobLoadException => true;
}