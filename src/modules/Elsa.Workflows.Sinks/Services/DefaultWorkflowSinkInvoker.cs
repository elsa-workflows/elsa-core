using Elsa.Common.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Sinks.Services;

/// <inheritdoc />
public class DefaultWorkflowSinkInvoker : IWorkflowSinkInvoker
{
    private readonly IEnumerable<IWorkflowSink> _workflowSinks;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultWorkflowSinkInvoker(IEnumerable<IWorkflowSink> workflowSinks, IWorkflowDefinitionService workflowDefinitionService, ILogger<DefaultWorkflowSinkInvoker> logger)
    {
        _workflowSinks = workflowSinks;
        _workflowDefinitionService = workflowDefinitionService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task InvokeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowState.DefinitionId, VersionOptions.SpecificVersion(workflowState.DefinitionVersion), cancellationToken);

        if (workflowDefinition == null) 
            throw new Exception($"No definition found for workflow state with definition ID of {workflowState.DefinitionId}");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var context = new WorkflowSinkContext(workflowState, workflow, cancellationToken);
        
        foreach (var workflowSink in _workflowSinks) 
            await InvokeAsync(workflowSink, context);
    }

    private async Task InvokeAsync(IWorkflowSink workflowSink, WorkflowSinkContext context)
    {
        try
        {
            await workflowSink.HandleWorkflowAsync(context);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "An exception occurred while invoking workflow sink {Sink}", workflowSink.GetType().Name);
        }
    }
}