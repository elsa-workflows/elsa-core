namespace Elsa.Testing.Shared;

public interface ISignalManager
{
    Task<T> WaitAsync<T>(object signal, int millisecondsTimeout = 200000);
    Task<object?> WaitAsync(object signal, int millisecondsTimeout = 200000);
    void Trigger(object signal, object? result = null);
}