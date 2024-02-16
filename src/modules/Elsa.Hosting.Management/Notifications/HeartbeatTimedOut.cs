using Elsa.Mediator.Contracts;

namespace Elsa.Hosting.Management.Notifications;

public record HeartbeatTimedOut(string InstanceName) : INotification;
