using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Services;

public class AIAuditSink(IServiceScopeFactory scopeFactory, ILogger<AIAuditSink> logger) : IAIAuditSink
{
    public async ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await RecordManyAsync([auditEvent], cancellationToken);
    }

    public async ValueTask RecordManyAsync(IReadOnlyCollection<AIAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        if (auditEvents.Count == 0)
            return;

        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IAIAuditEventHandler>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.RecordManyAsync(auditEvents, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogWarning(e, "AI audit handler {HandlerType} failed while recording {AuditEventCount} event(s).", handler.GetType().Name, auditEvents.Count);
            }
        }
    }
}
