using Elsa.Extensions;
using Elsa.Scripting.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.LogPersistence;
using JetBrains.Annotations;

namespace Elsa.Scripting.JavaScript.Handlers;

/// <summary>
/// A handler that configures the Jint engine with common types.
/// </summary>
[UsedImplicitly]
public class ConfigureEngineWithCommonTypes : INotificationHandler<EvaluatingJavaScript>
{
    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        
        // Add common .NET types.
        engine.RegisterType<DateTime>();
        engine.RegisterType<DateTimeOffset>();
        engine.RegisterType<TimeSpan>();
        engine.RegisterType<Guid>();
        engine.RegisterType<Random>();
        engine.RegisterType<LogPersistenceMode>();
        
        return Task.CompletedTask;
    }
}