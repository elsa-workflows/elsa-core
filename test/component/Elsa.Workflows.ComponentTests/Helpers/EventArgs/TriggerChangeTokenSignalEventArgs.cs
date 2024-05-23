namespace Elsa.Workflows.ComponentTests;

public class TriggerChangeTokenSignalEventArgs(string key) : EventArgs
{
    public string Key { get; } = key;
}