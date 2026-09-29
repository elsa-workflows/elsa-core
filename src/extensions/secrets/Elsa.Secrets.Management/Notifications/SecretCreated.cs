using Elsa.Mediator.Contracts;

namespace Elsa.Secrets.Management.Notifications;

public record SecretCreated(Secret Secret) : INotification;