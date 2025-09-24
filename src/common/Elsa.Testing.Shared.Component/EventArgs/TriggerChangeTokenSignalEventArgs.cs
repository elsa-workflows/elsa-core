namespace Elsa.Testing.Shared.EventArgs;

public class TriggerChangeTokenSignalEventArgs(string key) : System.EventArgs
{
    public string Key { get; } = key;
}