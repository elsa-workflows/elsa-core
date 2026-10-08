namespace Elsa.Testing.Shared;

public class TriggerChangeTokenSignalEventArgs(string key) : EventArgs
{
    public string Key { get; } = key;
}