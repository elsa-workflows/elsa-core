using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.JavaScript.Handlers;

[UsedImplicitly]
public class ConfigureEngineWithWorkflowVariableTypes(IOptions<ManagementOptions> options)
    : INotificationHandler<CreatingJavaScriptEngine>
{
    private static readonly Type[] BlacklistedTypes =
    [
        typeof(string),
        typeof(object),
        // Add more types if needed.
    ];

    /// <inheritdoc />
    public Task HandleAsync(CreatingJavaScriptEngine notification, CancellationToken cancellationToken)
    {
        var engineOptions = notification.Options;
        var variableTypes = options.Value.VariableDescriptors
            .Where(x => x.Type is { ContainsGenericParameters: false } && !BlacklistedTypes.Contains(x.Type) && !x.Type.IsPrimitive)
            .Select(x => x.Type);

        foreach (var variableType in variableTypes)
        {
            engineOptions.RegisterType(variableType);
        }

        return Task.CompletedTask;
    }
}
