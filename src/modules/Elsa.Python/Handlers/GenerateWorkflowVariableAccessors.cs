using System.Text;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Python.Notifications;
using Humanizer;
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
        sb.AppendLine("import System");
        sb.AppendLine("import clr");
        sb.AppendLine();
        sb.AppendLine("class WorkflowVariablesProxy:");
        sb.AppendLine("    def __init__(self, execution_context):");
        sb.AppendLine("        self.execution_context = execution_context");
        sb.AppendLine();

        foreach (var variable in variables)
        {
            var variableName = variable.Name;
            var variableType = variable.GetVariableType();
            var friendlyTypeName = GetFriendlyTypeName(variableType);
            sb.AppendLine($"    @property");
            sb.AppendLine($"    def {variableName}(self):");
            sb.AppendLine($"        return self.execution_context.GetVariable({friendlyTypeName}, '{variableName}')");
            sb.AppendLine($"    @{variableName}.setter");
            sb.AppendLine($"    def {variableName}(self, value):");
            sb.AppendLine($"        self.execution_context.SetVariable('{variableName}', value)");
        }

        sb.AppendLine();
        sb.AppendLine("variable = WorkflowVariablesProxy(execution_context);");

        var engine = notification.Engine;
        engine.Execute(sb.ToString(), notification.ScriptScope);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a friendly type name for the specified type that is suitable for constructing C# code.
    /// </summary>
    private static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName!;

        var sb = new StringBuilder();
        sb.Append(type.Namespace);
        sb.Append('.');
        sb.Append(type.Name[..type.Name.IndexOf('`')]);
        sb.Append('[');
        var genericArgs = type.GetGenericArguments();
        for (var i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(GetFriendlyTypeName(genericArgs[i]));
        }

        sb.Append(']');
        return sb.ToString();
    }
}