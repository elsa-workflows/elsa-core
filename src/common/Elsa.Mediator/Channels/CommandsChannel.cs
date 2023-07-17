using Elsa.Mediator.Abstractions;
using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Channels;

/// <inheritdoc cref="Elsa.Mediator.Contracts.ICommandsChannel" />
public class CommandsChannel : ChannelBase<ICommand>, ICommandsChannel
{
}