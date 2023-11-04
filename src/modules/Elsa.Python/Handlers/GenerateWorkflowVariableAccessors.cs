using System.Text;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Python.Notifications;
using JetBrains.Annotations;

namespace Elsa.Python.Handlers;

/// <summary>
/// Configures the C# evaluator with methods to access workflow variables.
/// </summary>
[UsedImplicitly]
public class GenerateWorkflowVariableAccessors : INotificationHandler<EvaluatingPython>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingPython notification, CancellationToken cancellationToken)
    {
        var expressionExecutionContext = notification.Context;
        var variables = expressionExecutionContext.GetVariablesInScope().ToList();
        var sb = new StringBuilder();
        sb.AppendLine("class WorkflowVariablesProxy:");
        sb.AppendLine("    def __init__(self, execution_context):");
        sb.AppendLine("        self.execution_context = execution_context");
        sb.AppendLine();

        foreach (var variable in variables)
        {
            var variableName = variable.Name;
            var variableType = variable.GetVariableType();
            var friendlyTypeName = variableType.GetFriendlyTypeName(Brackets.Square);
            sb.AppendLine($"    @property");
            sb.AppendLine($"    def {variableName}(self):");
            sb.AppendLine($"        return self.execution_context.GetVariable({friendlyTypeName}, '{variableName}')");
            sb.AppendLine($"    @{variableName}.setter");
            sb.AppendLine($"    def {variableName}(self, value):");
            sb.AppendLine($"        self.execution_context.SetVariable('{variableName}', value)");
        }

        sb.AppendLine();
        sb.AppendLine("variable = WorkflowVariablesProxy(execution_context);");

        notification.AppendScript(sb.ToString());

        return Task.CompletedTask;
    }
}