using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Alterations;

/// <summary>
/// Modifies a workflow variable.
/// </summary>
public class ModifyWorkflowVariable : AlterationBase
{
    /// <summary>
    /// The ID of the variable to modify.
    /// </summary>
    public string VariableId { get; set; } = default!;

    /// <summary>
    ///  The new value of the variable.
    /// </summary>
    public object? NewValue { get; set; }

    /// <inheritdoc />
    public override async ValueTask ApplyAsync(AlterationContext context, CancellationToken cancellationToken = default)
    {
        var workflow = context.Workflow;
        var variable = FindVariable(context, workflow, cancellationToken).Result;

        if (variable == null)
        {
            context.Fail($"Variable with ID {VariableId} not found");
            return;
        }
        
        await UpdateVariable(context, workflow, variable, cancellationToken);
        context.Succeed();
    }
    
    private async Task UpdateVariable(AlterationContext context, Workflow workflow, Variable variable, CancellationToken cancellationToken)
    {
        var variableStorage = variable.StorageDriverType ?? typeof(WorkflowStorageDriver);
        var storageDriverManager = context.ServiceProvider.GetRequiredService<IStorageDriverManager>();
        var storageDriver = storageDriverManager.Get(variableStorage)!;
        //var workflowExecutionContextFactory = context.ServiceProvider.GetRequiredService<IWorkflowExecutionContextFactory>();
        //var workflowInstance = context.WorkflowInstance;
        //var workflowExecutionContext = await workflowExecutionContextFactory.CreateAsync(context.ServiceProvider, workflow, workflowInstance.Id, workflowInstance.WorkflowState);
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var storageDriverContext = new StorageDriverContext(workflowExecutionContext, cancellationToken);
        var newValue = NewValue;
        await storageDriver.WriteAsync(variable.Id, newValue!, storageDriverContext);
    }

    // private async Task<Workflow?> FindWorkflowAsync(AlterationContext context)
    // {
    //     var cancellationToken = context.CancellationToken;
    //     var workflowDefinition = await FindWorkflowDefinition(context, cancellationToken);
    //     
    //     if (workflowDefinition == null)
    //         return null;
    //
    //     var workflowDefinitionService = context.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
    //     return await workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
    // }
    //
    // private async Task<WorkflowDefinition?> FindWorkflowDefinition(AlterationContext context, CancellationToken cancellationToken)
    // {
    //     var workflowInstance = context.WorkflowInstance;
    //     var definitionVersionId = workflowInstance.DefinitionVersionId;
    //     var workflowDefinitionStore = context.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
    //     var workflowDefinition = await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = definitionVersionId }, cancellationToken);
    //     return workflowDefinition;
    // }

    private async Task<Variable?> FindVariable(AlterationContext context, Workflow workflow, CancellationToken cancellationToken)
    {
        var activityVisitor = context.ServiceProvider.GetRequiredService<IActivityVisitor>();
        var useActivityIdAsNodeId = workflow.CreatedWithModernTooling();
        var root = workflow.Root;
        var graph = await activityVisitor.VisitAsync(root, useActivityIdAsNodeId, cancellationToken);
        var flattenedList = graph.Flatten().ToList();
        return flattenedList.Where(x => x.Activity is IVariableContainer).SelectMany(x => ((IVariableContainer)x.Activity).Variables).FirstOrDefault(x => x.Id == VariableId);
    }
}