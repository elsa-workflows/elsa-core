namespace Elsa.Workflows.ComponentTests;

public interface ITriggerChangeTokenSignalEvents
{
    event EventHandler<TriggerChangeTokenSignalEventArgs> ChangeTokenSignalTriggered;
    
    void OnChangeTokenSignalTriggered(TriggerChangeTokenSignalEventArgs args);
}