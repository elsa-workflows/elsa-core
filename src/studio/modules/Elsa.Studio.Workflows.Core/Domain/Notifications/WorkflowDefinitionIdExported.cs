using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition id is exported.
/// </summary>
public record WorkflowDefinitionIdExported(string WorkflowDefinitionId, VersionOptions? VersionOptions, FileDownload FileDownload) : INotification;