using System.Text;
using Elsa.CSharp.Notifications;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.CSharp.Handlers;

/// <summary>
/// Configures the C# evaluator with methods to access workflow variables.
/// </summary>
[UsedImplicitly]
public class GenerateArgumentAccessors : INotificationHandler<EvaluatingCSharp>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingCSharp notification, CancellationToken cancellationToken)
    {
        var arguments = notification.Options.Arguments;
        var sb = new StringBuilder();
        sb.AppendLine("public partial class ArgumentsProxy {");
        sb.AppendLine("\tpublic ArgumentsProxy(IDictionary<string, object> arguments) => Arguments = arguments;");
        sb.AppendLine("\tpublic IDictionary<string, object> Arguments { get; }");
        sb.AppendLine();
        sb.AppendLine("\tpublic T? Get<T>(string name) => Arguments.TryGetValue(name, out var v) ? (T?)v : default;");
        sb.AppendLine();
        foreach (var argument in arguments)
        {
            // var d = new Dictionary<string, object>();
            // d.TryGetValue("", out var f);
            var argumentName = argument.Key;
            var variableType = argument.Value.GetType();
            var friendlyTypeName = variableType.GetFriendlyTypeName(Brackets.Angle);
            sb.AppendLine($"\tpublic {friendlyTypeName} {argumentName}");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\tget => Get<{friendlyTypeName}>(\"{argumentName}\");");
            sb.AppendLine("\t}");
        }

        sb.AppendLine("}");
        sb.AppendLine("var Args = new ArgumentsProxy(Arguments);");
        notification.AppendScript(sb.ToString());
        return Task.CompletedTask;
    }
}