using System.Diagnostics.CodeAnalysis;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Workflows;
using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime;
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
    private readonly IJsonSerializer _jsonSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanScheduler"/> class.
    /// </summary>
    public DefaultAlterationPlanScheduler(IWorkflowDispatcher workflowDispatcher, IIdentityGenerator identityGenerator, IJsonSerializer jsonSerializer)
    {
        _workflowDispatcher = workflowDispatcher;
        _identityGenerator = identityGenerator;
        _jsonSerializer = jsonSerializer;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type of the object to be deserialized is not known at compile time.")]
    public async Task<string> SubmitAsync(AlterationPlanParams planParams, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(planParams.Id))
            planParams.Id = _identityGenerator.GenerateId();
        
        var definitionId = ExecuteAlterationPlanWorkflow.WorkflowDefinitionId;
        var serializedPlan = _jsonSerializer.Serialize(planParams);
        var request = new DispatchWorkflowDefinitionRequest(definitionId, VersionOptions.Published)
        {
            Input = new Dictionary<string, object>
            {
                ["Plan"] = serializedPlan
            }
        };
        await _workflowDispatcher.DispatchAsync(request, cancellationToken);

        return planParams.Id;
    }
}