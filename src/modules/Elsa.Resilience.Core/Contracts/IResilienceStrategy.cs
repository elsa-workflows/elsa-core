namespace Elsa.Resilience;

public interface IResilienceStrategy
{
    string Id { get; set; }
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);
}