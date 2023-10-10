using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Expressions.Helpers;
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
    protected override async ValueTask HandleAsync(AlterationContext context, ModifyVariable alteration)
    {
        var workflow = context.Workflow;
        var cancellationToken = context.CancellationToken;
        var variable = await FindVariable(context, alteration, workflow, cancellationToken);

        if (variable == null)
        {
            context.Fail($"Variable with ID {alteration.VariableId} not found");
            return;
        }

        var convertedValue = ConvertValue(variable, alteration.Value);
        await UpdateVariable(context, variable, convertedValue, cancellationToken);
        context.Succeed();
    }

    private object? ConvertValue(Variable variable, object? value)
    {
        var variableType = variable.GetType();
        
        // If the variable has a type, convert the value into that type.
        if(variableType.GenericTypeArguments.Length != 1)
            return value;
        
        var type = variableType.GenericTypeArguments[0];
        return value.ConvertTo(type);
    }

    private async Task UpdateVariable(AlterationContext context, Variable variable, object? value, CancellationToken cancellationToken)
    {
        var variableStorage = variable.StorageDriverType ?? typeof(WorkflowStorageDriver);
        var storageDriverManager = context.ServiceProvider.GetRequiredService<IStorageDriverManager>();
        var storageDriver = storageDriverManager.Get(variableStorage)!;
        var activityExecutionContext = FindActivityExecutionContextContainingVariable(context, variable);
        
        if (activityExecutionContext == null)
        {
            context.Fail($"Activity execution context containing variable with ID {variable.Id} not found");
            return;
        }
        
        var storageDriverContext = new StorageDriverContext(activityExecutionContext, cancellationToken);
        var stateId = GetStateId(variable);
        await storageDriver.WriteAsync(stateId, value!, storageDriverContext);
    }

    private ActivityExecutionContext? FindActivityExecutionContextContainingVariable(AlterationContext context, Variable variable)
    {
        var query =
            from activityExecutionContext in context.WorkflowExecutionContext.ActivityExecutionContexts
            from var in activityExecutionContext.Variables
            where var.Id == variable.Id
            select activityExecutionContext;
        
        return query.FirstOrDefault();
    }

    private async Task<Variable?> FindVariable(AlterationContext context, ModifyVariable alteration, Workflow workflow, CancellationToken cancellationToken)
    {
        var activityVisitor = context.ServiceProvider.GetRequiredService<IActivityVisitor>();
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