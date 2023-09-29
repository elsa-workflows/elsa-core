using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations;

/// <summary>
/// Modifies a workflow variable.
/// </summary>
public class ModifyVariable : AlterationBase
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
        
        await UpdateVariable(context, variable, cancellationToken);
        context.Succeed();
    }
    
    private async Task UpdateVariable(AlterationContext context, Variable variable, CancellationToken cancellationToken)
    {
        var variableStorage = variable.StorageDriverType ?? typeof(WorkflowStorageDriver);
        var storageDriverManager = context.ServiceProvider.GetRequiredService<IStorageDriverManager>();
        var storageDriver = storageDriverManager.Get(variableStorage)!;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var storageDriverContext = new StorageDriverContext(workflowExecutionContext, cancellationToken);
        var newValue = NewValue;
        await storageDriver.WriteAsync(variable.Id, newValue!, storageDriverContext);
    }

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