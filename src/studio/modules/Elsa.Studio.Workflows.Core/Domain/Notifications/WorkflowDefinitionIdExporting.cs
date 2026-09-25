using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition id is exporting.
/// </summary>
public record WorkflowDefinitionIdExporting(string WorkflowDefinitionId, VersionOptions? VersionOptions) : INotification;