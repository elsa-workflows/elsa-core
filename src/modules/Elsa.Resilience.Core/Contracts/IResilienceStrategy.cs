namespace Elsa.Resilience;

public interface IResilienceStrategy
{
    string Id { get; }
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);
}