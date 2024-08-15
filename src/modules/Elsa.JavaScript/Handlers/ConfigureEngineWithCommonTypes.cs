using Elsa.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.JavaScript.Handlers;

/// A handler that configures the Jint engine with common types.
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
        
        return Task.CompletedTask;
    }
}