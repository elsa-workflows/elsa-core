using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Services;

public class AiAuditSink(IServiceScopeFactory scopeFactory) : IAiAuditSink
{
    public async ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IAiAuditEventHandler>();

        foreach (var handler in handlers)
            await handler.RecordAsync(auditEvent, cancellationToken);
    }
}

public interface IAiAuditEventHandler
{
    ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
