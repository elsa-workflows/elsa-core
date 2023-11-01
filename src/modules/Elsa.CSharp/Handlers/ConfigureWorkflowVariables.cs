using System.Text;
using Elsa.CSharp.Notifications;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Humanizer;
using JetBrains.Annotations;

namespace Elsa.CSharp.Handlers;

/// <summary>
/// Configures the C# evaluator with methods to access workflow variables.
/// </summary>
[UsedImplicitly]
public class ConfigureWorkflowVariables : INotificationHandler<EvaluatingCSharp>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingCSharp notification, CancellationToken cancellationToken)
    {
        var expressionExecutionContext = notification.Context;
        var variables = expressionExecutionContext.GetVariablesInScope().ToList();
        var sb = new StringBuilder();
        sb.AppendLine("public class WorkflowVariablesWrapper {");
        sb.AppendLine("\tpublic WorkflowVariablesWrapper(ExecutionContextProxy context) => Context = context;");
        sb.AppendLine("\tpublic ExecutionContextProxy Context { get; }");

        foreach (var variable in variables)
        {
            var variableName = variable.Name.Pascalize();
            var variableType = variable.GetVariableType();
            var friendlyTypeName = GetFriendlyTypeName(variableType);
            sb.AppendLine($"\tpublic {friendlyTypeName} {variableName}");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tget => Context.GetVariable<{friendlyTypeName}>(\"{variableName}\");");
            sb.AppendLine($"\t\tset => Context.SetVariable(\"{variableName}\", value);");
            sb.AppendLine("\t}");
        }

        sb.AppendLine("}");
        sb.AppendLine("var Variables = new WorkflowVariablesWrapper(Context);");
        notification.AppendScript(sb.ToString());
        return Task.CompletedTask;
    }
    
    static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.FullName!;

        var sb = new StringBuilder();
        sb.Append(type.Namespace);
        sb.Append('.');
        sb.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
        sb.Append('<');
        var genericArgs = type.GetGenericArguments();
        for (var i = 0; i < genericArgs.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(GetFriendlyTypeName(genericArgs[i]));
        }
        sb.Append('>');
        return sb.ToString();
    }
}