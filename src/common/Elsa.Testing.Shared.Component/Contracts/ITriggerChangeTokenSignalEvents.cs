namespace Elsa.Testing.Shared;

public interface ITriggerChangeTokenSignalEvents
{
    event EventHandler<TriggerChangeTokenSignalEventArgs> ChangeTokenSignalTriggered;
    
    void OnChangeTokenSignalTriggered(TriggerChangeTokenSignalEventArgs args);
}