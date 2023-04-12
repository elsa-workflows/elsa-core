using JetBrains.Annotations;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a command.
/// </summary>
[PublicAPI]
public interface ICommand
{
}

/// <summary>
/// Represents a command.
/// </summary>
/// <typeparam name="T">The type of the result.</typeparam>
[PublicAPI]
public interface ICommand<T> : ICommand
{
}