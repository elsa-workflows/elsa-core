using Elsa.Mediator.Contracts;
using Elsa.Python.Models;
using Elsa.Python.Notifications;
using JetBrains.Annotations;

namespace Elsa.Python.Handlers;

/// <summary>
/// Adds input accessors to the Python engine.
/// </summary>
[UsedImplicitly]
public class AddInputAccessors : INotificationHandler<EvaluatingPython>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingPython notification, CancellationToken cancellationToken)
    {
        var scope = notification.ScriptScope;
        var inputProxy = new InputProxy(notification.Context);
        scope.SetVariable("input", inputProxy);
        return Task.CompletedTask;
    }
}