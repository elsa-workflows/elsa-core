namespace Elsa.Mediator.Contracts;

public interface IMediator : IRequestSender, ICommandSender, IEventPublisher
{
}