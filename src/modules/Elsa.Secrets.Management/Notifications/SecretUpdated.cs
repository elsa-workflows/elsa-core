using Elsa.Mediator.Contracts;

namespace Elsa.Secrets.Management.Notifications;

public record SecretUpdated(Secret Secret) : INotification;