namespace Elsa.Workflows.ComponentTests;

public interface ISignalManager
{
    Task<T> WaitAsync<T>(object signal, int millisecondsTimeout = 1000);
    Task<object?> WaitAsync(object signal, int millisecondsTimeout = 1000);
    void Trigger(object signal, object? result = null);
}