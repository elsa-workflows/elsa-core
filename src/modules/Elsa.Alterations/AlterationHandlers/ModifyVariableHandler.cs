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

namespace Elsa.Alterations.AlterationHandlers;

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
        var variable = await FindVariable(context, alteration, workflow, cancellationToken);

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
        var activityExecutionContext = FindActivityExecutionContextContainingVariable(handlerContext, variable);
        
        if (activityExecutionContext == null)
        {
            handlerContext.Fail($"Activity execution context containing variable with ID {variable.Id} not found");
            return;
        }
        
        var storageDriverContext = new StorageDriverContext(activityExecutionContext, cancellationToken);
        var newValue = alteration.Value;
        var stateId = GetStateId(variable);
        await storageDriver.WriteAsync(stateId, newValue!, storageDriverContext);
    }

    private ActivityExecutionContext? FindActivityExecutionContextContainingVariable(AlterationHandlerContext context, Variable variable)
    {
        var query =
            from activityExecutionContext in context.WorkflowExecutionContext.ActiveActivityExecutionContexts
            from var in activityExecutionContext.Variables
            where var.Id == variable.Id
            select activityExecutionContext;
        
        return query.FirstOrDefault();
    }

    private async Task<Variable?> FindVariable(AlterationHandlerContext handlerContext, ModifyVariable alteration, Workflow workflow, CancellationToken cancellationToken)
    {
        var activityVisitor = handlerContext.ServiceProvider.GetRequiredService<IActivityVisitor>();
        var useActivityIdAsNodeId = workflow.CreatedWithModernTooling();
        var graph = await activityVisitor.VisitAsync(workflow, useActivityIdAsNodeId, cancellationToken);
        var flattenedList = graph.Flatten().ToList();

        return flattenedList
            .Where(x => x.Activity is IVariableContainer)
            .SelectMany(x => ((IVariableContainer)x.Activity).Variables)
            .FirstOrDefault(x => x.Id == alteration.VariableId);
    }

    private string GetStateId(Variable variable) => variable.Id;
}