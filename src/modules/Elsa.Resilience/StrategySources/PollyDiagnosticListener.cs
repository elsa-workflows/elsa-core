using System.Diagnostics;

namespace Elsa.Resilience.StrategySources;

public class PollyDiagnosticListener : IObserver<DiagnosticListener>
{
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "Polly")
            listener.Subscribe(new RetryEventObserver());
    }
    public void OnError(Exception _) { }
    public void OnCompleted() { }
}