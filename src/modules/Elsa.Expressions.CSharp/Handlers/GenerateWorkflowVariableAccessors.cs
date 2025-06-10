using System.Dynamic;
using System.Text;
using Elsa.Expressions.CSharp.Extensions;
using Elsa.Expressions.CSharp.Notifications;
using Elsa.Expressions.CSharp.Options;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.CSharp.Handlers;

/// <summary>
/// Configures the C# evaluator with methods to access workflow variables.
/// </summary>
[UsedImplicitly]
public class GenerateWorkflowVariableAccessors(IOptions<CSharpOptions> options) : INotificationHandler<EvaluatingCSharp>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingCSharp notification, CancellationToken cancellationToken)
    {
        var expressionExecutionContext = notification.Context;
        var variables = expressionExecutionContext.GetVariablesInScope().ToList();
        var sb = new StringBuilder();
        sb.AppendLine("public partial class WorkflowVariablesProxy {");
        sb.AppendLine("\tpublic WorkflowVariablesProxy(ExecutionContextProxy executionContext) => ExecutionContext = executionContext;");
        sb.AppendLine("\tpublic ExecutionContextProxy ExecutionContext { get; }");
        sb.AppendLine();
        sb.AppendLine("\tpublic T? Get<T>(string name) => ExecutionContext.GetVariable<T>(name);");
        sb.AppendLine("\tpublic object? Get(string name) => ExecutionContext.GetVariable(name);");
        sb.AppendLine("\tpublic void Set(string name, object? value) => ExecutionContext.SetVariable(name, value);");
        sb.AppendLine();

        if (!options.Value.DisableWrappers)
        {
            foreach (var variable in variables.Where(x => x.Name.IsValidVariableName()))
            {
                var variableName = variable.Name.Pascalize();
                var variableType = variable.GetVariableType();
                
                // Check if the variable type is ExpandoObject
                bool isExpandoObject = variableType == typeof(ExpandoObject);
                
                // Use dynamic type for ExpandoObject to enable dot notation but keep the original type for retrieval
                var displayTypeName = isExpandoObject ? "dynamic" : variableType.GetFriendlyTypeName(Brackets.Angle);
                var retrieveTypeName = isExpandoObject ? "ExpandoObject" : displayTypeName;
                
                sb.AppendLine($"\tpublic {displayTypeName} {variableName}");
                sb.AppendLine("\t{");
                sb.AppendLine($"\t\tget => Get<{retrieveTypeName}>(\"{variableName}\");");
                sb.AppendLine($"\t\tset => Set(\"{variableName}\", value);");
                sb.AppendLine("\t}");
            }
        }

        sb.AppendLine("}");
        sb.AppendLine("var Variables = new WorkflowVariablesProxy(ExecutionContext);"); // Obsolete; use Variable instead.
        sb.AppendLine("var Variable = Variables;");
        notification.AppendScript(sb.ToString());
        return Task.CompletedTask;
    }
}