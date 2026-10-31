using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An <see cref="IWorkflowDefinitionService"/> stub that lets a test control when an asynchronous name
/// uniqueness check completes and records how the create dialog invoked it. Every member other than
/// <see cref="GetIsNameUniqueAsync"/> and <see cref="CreateNewDefinitionAsync(string,string?,string?,Action{SaveWorkflowDefinitionRequest}?,CancellationToken)"/>
/// is unused by the tests that consume this stub and throws to flag an unexpected call.
/// </summary>
internal sealed class ControlledWorkflowDefinitionService : ThrowingWorkflowDefinitionServiceBase
{
    private readonly Queue<PendingValidation> _pendingValidations = new();

    public int CreateCallCount { get; private set; }
    public string? CreatedName { get; private set; }

    public PendingValidation EnqueueValidation()
    {
        var validation = new PendingValidation();
        lock (_pendingValidations)
            _pendingValidations.Enqueue(validation);
        return validation;
    }

    public override async Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default)
    {
        PendingValidation validation;
        lock (_pendingValidations)
            validation = _pendingValidations.Dequeue();

        validation.Name = name;
        validation.Started.TrySetResult(true);
        return await validation.Result.Task.WaitAsync(cancellationToken);
    }

    public override Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description, string? rootActivityTemplateKey, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default)
    {
        CreateCallCount++;
        CreatedName = name;
        return Task.FromResult(new Result<WorkflowDefinition, ValidationErrors>(new WorkflowDefinition
        {
            Name = name,
            Description = description
        }));
    }

    public sealed class PendingValidation
    {
        public string? Name { get; set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
