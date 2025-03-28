using System.Diagnostics.CodeAnalysis;
using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Modifies a workflow variable.
/// </summary>
[UsedImplicitly]
public class ModifyVariableHandler : AlterationHandlerBase<ModifyVariable>
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
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

        var convertedValue = variable.ParseValue(alteration.Value);
        UpdateVariable(context, variable, convertedValue);
        context.Succeed();
    }

    private void UpdateVariable(AlterationContext context, Variable variable, object? value)
    {
        var activityExecutionContext = FindActivityExecutionContextContainingVariable(context, variable);

        if (activityExecutionContext == null)
        {
            context.Fail($"Activity execution context containing variable with ID {variable.Id} not found");
            return;
        }

        variable.Set(activityExecutionContext, value);
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
        var graph = await activityVisitor.VisitAsync(workflow, cancellationToken);
        var flattenedList = graph.Flatten().ToList();

        return flattenedList
            .Where(x => x.Activity is IVariableContainer)
            .SelectMany(x => ((IVariableContainer)x.Activity).Variables)
            .FirstOrDefault(x => x.Id == alteration.VariableId);
    }
}