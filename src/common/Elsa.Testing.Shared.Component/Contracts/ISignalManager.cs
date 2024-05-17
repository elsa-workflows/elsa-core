namespace Elsa.Testing.Shared;

public interface ISignalManager
{
    Task<T> WaitAsync<T>(object signal, int millisecondsTimeout = 5000);
    Task<object?> WaitAsync(object signal, int millisecondsTimeout = 5000);
    void Trigger(object signal, object? result = null);
}