using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Saves a compensation draft only while the backend context that loaded it remains selected.
/// </summary>
public interface IContextBoundWorkflowDefinitionEditorService
{
    Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveNativeCompensationDraftAsync(
        WorkflowDefinition workflowDefinition,
        bool publish,
        string expectedBackendContextKey,
        Func<WorkflowDefinition, Task>? workflowSavedCallback = null,
        CancellationToken cancellationToken = default);
}
