namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a request.
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Represents a request.
/// </summary>
/// <typeparam name="T">The type of the response.</typeparam>
public interface IRequest<T> : IRequest
{
}