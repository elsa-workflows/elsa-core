using Elsa.Mediator.Contracts;

namespace Elsa.Secrets.Management.Notifications;

public record SecretExpired(Secret Secret) : INotification;