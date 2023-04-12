using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Notifications;

/// <summary>
/// Notifies that an activity descriptor is being registered.
/// </summary>
public record ActivityDescriptorRegistering(ActivityDescriptor ActivityDescriptor) : INotification;