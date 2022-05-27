namespace Elsa.Mediator.Services;

public interface IMediator : IRequestSender, ICommandSender, IEventPublisher
{
}