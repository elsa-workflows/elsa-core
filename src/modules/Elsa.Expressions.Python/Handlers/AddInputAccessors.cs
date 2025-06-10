using Elsa.Mediator.Contracts;
using Elsa.Expressions.Python.Models;
using Elsa.Expressions.Python.Notifications;
using JetBrains.Annotations;

namespace Elsa.Expressions.Python.Handlers;

/// <summary>
/// Adds input accessors to the Python engine.
/// </summary>
[UsedImplicitly]
public class AddInputAccessors : INotificationHandler<EvaluatingPython>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingPython notification, CancellationToken cancellationToken)
    {
        var scope = notification.Scope;
        var inputProxy = new InputProxy(notification.Context);
        scope.Set("input", inputProxy);
        return Task.CompletedTask;
    }
}