using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Modifies a workflow variable.
/// </summary>
public class ModifyVariableHandler : AlterationHandlerBase<ModifyVariable>
{
    /// <inheritdoc />
    protected override async ValueTask HandleAsync(AlterationHandlerContext context, ModifyVariable alteration)
    {
        var workflow = context.Workflow;
        var cancellationToken = context.CancellationToken;
        var variable = FindVariable(context, alteration, workflow, cancellationToken).Result;

        if (variable == null)
        {
            context.Fail($"Variable with ID {alteration.VariableId} not found");
            return;
        }
        
        await UpdateVariable(context, alteration, variable, cancellationToken);
        context.Succeed();
    }
    
    private async Task UpdateVariable(AlterationHandlerContext handlerContext, ModifyVariable alteration, Variable variable, CancellationToken cancellationToken)
    {
        var variableStorage = variable.StorageDriverType ?? typeof(WorkflowStorageDriver);
        var storageDriverManager = handlerContext.ServiceProvider.GetRequiredService<IStorageDriverManager>();
        var storageDriver = storageDriverManager.Get(variableStorage)!;
        var workflowExecutionContext = handlerContext.WorkflowExecutionContext;
        var storageDriverContext = new StorageDriverContext(workflowExecutionContext, cancellationToken);
        var newValue = alteration.NewValue;
        await storageDriver.WriteAsync(variable.Id, newValue!, storageDriverContext);
    }

    private async Task<Variable?> FindVariable(AlterationHandlerContext handlerContext, ModifyVariable alteration, Workflow workflow, CancellationToken cancellationToken)
    {
        var activityVisitor = handlerContext.ServiceProvider.GetRequiredService<IActivityVisitor>();
        var useActivityIdAsNodeId = workflow.CreatedWithModernTooling();
        var root = workflow.Root;
        var graph = await activityVisitor.VisitAsync(root, useActivityIdAsNodeId, cancellationToken);
        var flattenedList = graph.Flatten().ToList();
        return flattenedList.Where(x => x.Activity is IVariableContainer).SelectMany(x => ((IVariableContainer)x.Activity).Variables).FirstOrDefault(x => x.Id == alteration.VariableId);
    }
}