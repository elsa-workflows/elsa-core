using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Expressions.JavaScript.TypeDefinitions.Abstractions;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Secrets.Contracts;
using JetBrains.Annotations;

namespace Elsa.Secrets.JavaScript.Scripting.JavaScript;

[UsedImplicitly]
public class SecretsJavaScriptHandler(ISecretResolver secretResolver) : FunctionDefinitionProvider, INotificationHandler<EvaluatingJavaScript>
{
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var context = notification.Context;
        notification.Engine.SetValue("getSecret", (Func<string, Task<string>>)(name => secretResolver.ResolveAsync(name, context.CancellationToken)));
        return Task.CompletedTask;
    }

    protected override IEnumerable<FunctionDefinition> GetFunctionDefinitions(TypeDefinitionContext context)
    {
        yield return CreateFunctionDefinition(function => function
            .Name("getSecret")
            .ReturnType("Promise<string>")
            .Parameter("name", "string"));
    }
}
