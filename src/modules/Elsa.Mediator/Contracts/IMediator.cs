namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a mediator that can send requests, commands and publish events.
/// </summary>
public interface IMediator : IRequestSender, ICommandSender, IEventPublisher
{
}