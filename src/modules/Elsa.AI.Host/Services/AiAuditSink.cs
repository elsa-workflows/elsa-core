using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Services;

public class AiAuditSink(IServiceScopeFactory scopeFactory, ILogger<AiAuditSink> logger) : IAiAuditSink
{
    public async ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await RecordManyAsync([auditEvent], cancellationToken);
    }

    public async ValueTask RecordManyAsync(IReadOnlyCollection<AiAuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        if (auditEvents.Count == 0)
            return;

        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IAiAuditEventHandler>();

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
