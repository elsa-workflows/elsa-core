namespace Elsa.Testing.Shared.Services;

public class TriggerChangeTokenSignalEvents
{
    public event EventHandler<TriggerChangeTokenSignalEventArgs>? ChangeTokenSignalTriggered;
    public void RaiseChangeTokenSignalTriggered(TriggerChangeTokenSignalEventArgs args) => ChangeTokenSignalTriggered?.Invoke(this, args);
}