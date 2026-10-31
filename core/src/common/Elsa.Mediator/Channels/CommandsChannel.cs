using Elsa.Mediator.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;

namespace Elsa.Mediator.Channels;

/// <inheritdoc cref="Elsa.Mediator.Contracts.ICommandsChannel" />
public class CommandsChannel : ChannelBase<CommandContext>, ICommandsChannel
{
}