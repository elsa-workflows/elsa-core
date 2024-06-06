namespace Elsa.Workflows.ComponentTests;

public interface ISignalManager
{
    Task<T> WaitAsync<T>(object signal, int millisecondsTimeout = 2000);
    Task<object?> WaitAsync(object signal, int millisecondsTimeout = 2000);
    void Trigger(object signal, object? result = null);
}