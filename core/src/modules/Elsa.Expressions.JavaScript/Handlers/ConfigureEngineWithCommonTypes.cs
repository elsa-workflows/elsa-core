using Elsa.Extensions;
using Elsa.Expressions.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.LogPersistence;
using JetBrains.Annotations;

namespace Elsa.Expressions.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with common types.
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithCommonTypes : INotificationHandler<CreatingJavaScriptEngine>
{
    /// <inheritdoc />
    public Task HandleAsync(CreatingJavaScriptEngine notification, CancellationToken cancellationToken)
    {
        var options = notification.Options;

        // Add common .NET types. Each one is described the first time a script reads its name.
        options.RegisterType<DateTime>();
        options.RegisterType<DateTimeOffset>();
        options.RegisterType<TimeSpan>();
        options.RegisterType<Guid>();
        options.RegisterType<Random>();
        options.RegisterType<LogPersistenceMode>();

        return Task.CompletedTask;
    }
}
