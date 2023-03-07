using Elsa.Jobs.Contracts;
using Elsa.Mediator.Contracts;

namespace Elsa.Jobs.Notifications;

public record JobExecuted(IJob Job) : INotification;