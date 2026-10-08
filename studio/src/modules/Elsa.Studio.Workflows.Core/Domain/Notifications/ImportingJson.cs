using Elsa.Studio.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Notifications;

[UsedImplicitly]
/// <summary>
/// Represents the notification published when an importing is json.
/// </summary>
public record ImportingJson(string Json) : INotification;