using System.Text;
using Elsa.CSharp.Extensions;
using Elsa.CSharp.Notifications;
using Elsa.CSharp.Options;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.CSharp.Handlers;

/// <summary>
/// Configures the C# evaluator with methods to access workflow variables.
/// </summary>
[UsedImplicitly]
public class GenerateWorkflowInputAccessors(IOptions<CSharpOptions> options) : INotificationHandler<EvaluatingCSharp>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingCSharp notification, CancellationToken cancellationToken)
    {
        var expressionExecutionContext = notification.Context;

        if (!expressionExecutionContext.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
            return Task.CompletedTask;

        var workflow = workflowExecutionContext.Workflow;
        var inputDefinitions = workflow.Inputs.ToList();
        var sb = new StringBuilder();
        sb.AppendLine("public partial class WorkflowInputsProxy {");
        sb.AppendLine("\tpublic WorkflowInputsProxy(ExecutionContextProxy executionContext) => ExecutionContext = executionContext;");
        sb.AppendLine("\tpublic ExecutionContextProxy ExecutionContext { get; }");
        sb.AppendLine();
        sb.AppendLine("\tpublic T? Get<T>(string name) => ExecutionContext.GetInput<T>(name);");
        sb.AppendLine();

        if (!options.Value.DisableWrappers)
        {
            foreach (var inputDefinition in inputDefinitions.Where(x => x.Name.IsValidVariableName()))
            {
                var inputName = inputDefinition.Name;
                var variableType = inputDefinition.Type;
                var friendlyTypeName = variableType.GetFriendlyTypeName(Brackets.Angle);
                sb.AppendLine($"\tpublic {friendlyTypeName} {inputName}");
                sb.AppendLine("\t{");
                sb.AppendLine($"\t\tget => Get<{friendlyTypeName}>(\"{inputName}\");");
                sb.AppendLine("\t}");
            }
        }

        sb.AppendLine("}");
        sb.AppendLine("var Inputs = new WorkflowInputsProxy(ExecutionContext);");
        notification.AppendScript(sb.ToString());
        return Task.CompletedTask;
    }
}