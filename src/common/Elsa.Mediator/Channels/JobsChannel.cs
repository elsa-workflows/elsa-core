using Elsa.Mediator.Abstractions;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;

namespace Elsa.Mediator.Channels;

/// <inheritdoc cref="Elsa.Mediator.Contracts.IJobsChannel" />
public class JobsChannel : ChannelBase<EnqueuedJob>, IJobsChannel
{
}