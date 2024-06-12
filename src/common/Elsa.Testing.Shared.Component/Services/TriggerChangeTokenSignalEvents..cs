namespace Elsa.Testing.Shared.Services;

public class TriggerChangeTokenSignalEvents : ITriggerChangeTokenSignalEvents
{
    public event EventHandler<TriggerChangeTokenSignalEventArgs>? ChangeTokenSignalTriggered;
    public void OnChangeTokenSignalTriggered(TriggerChangeTokenSignalEventArgs args) => ChangeTokenSignalTriggered?.Invoke(this, args);
}