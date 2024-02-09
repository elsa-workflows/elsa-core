using Elsa.Mediator.Contracts;

namespace Elsa.Hosting.Management.Notifications;

public record InstanceDeactivated(string InstanceName) : INotification;
