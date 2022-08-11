using Elsa.Jobs.Services;
using Elsa.Mediator.Services;

namespace Elsa.Jobs.Notifications;

public record JobExecuted(IJob Job) : INotification;