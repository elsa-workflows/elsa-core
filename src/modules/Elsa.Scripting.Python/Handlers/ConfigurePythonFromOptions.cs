using Elsa.Mediator.Contracts;
using Elsa.Scripting.Python.Notifications;
using Elsa.Scripting.Python.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Scripting.Python.Handlers;

/// <summary>
/// This handler configures the Python engine based on the <see cref="PythonOptions"/>.
/// </summary>
[UsedImplicitly]
public class ConfigurePythonFromOptions : INotificationHandler<EvaluatingPython>
{
    private readonly PythonOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurePythonFromOptions"/> class.
    /// </summary>
    public ConfigurePythonFromOptions(IOptions<PythonOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task HandleAsync(EvaluatingPython notification, CancellationToken cancellationToken)
    {
        foreach (var action in _options.Scopes) action(notification.Scope);
        foreach (var script in _options.Scripts) notification.AppendScript(script);

        return Task.CompletedTask;
    }
}