using Elsa.Mediator.Contracts;

namespace Elsa.Secrets.Management.Notifications;

public record SecretDeleted(Secret Secret) : INotification;