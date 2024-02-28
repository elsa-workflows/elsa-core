using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Workflows;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Alterations.Services;

/// <summary>
/// Stores the new plan and schedules it for immediate execution.
/// </summary>
public class DefaultAlterationPlanScheduler : IAlterationPlanScheduler
{
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanScheduler"/> class.
    /// </summary>
    public DefaultAlterationPlanScheduler(IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator)
    {
        _workflowDispatcher = workflowDispatcher;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public async Task<string> SubmitAsync(AlterationPlanParams planParams, CancellationToken cancellationToken = default)
    {
        var planId = !string.IsNullOrWhiteSpace(planParams.Id) ? planParams.Id : _identityGenerator.GenerateId();
        var definitionId = ExecuteAlterationPlanWorkflow.WorkflowDefinitionId;
        var request = new DispatchWorkflowDefinitionRequest(definitionId, VersionOptions.Published);
        await _workflowDispatcher.DispatchAsync(request, cancellationToken);

        return planId;
    }
}