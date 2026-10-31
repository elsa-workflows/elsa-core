using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.Domain.Notifications;

[UsedImplicitly]
/// <summary>
/// Represents the notification published when an imported is json.
/// </summary>
public record ImportedJson(string Json) : INotification;