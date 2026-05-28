using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAITool : IDisposable
{
    AIToolDefinition Definition { get; }
    ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default);
    void IDisposable.Dispose()
    {
    }
}

public interface IAIToolRegistry
{
    ValueTask<IReadOnlyCollection<AIToolDefinition>> ListAsync(AIToolQuery query, CancellationToken cancellationToken = default);
    ValueTask<IAITool?> FindAsync(string name, AIToolQuery query, CancellationToken cancellationToken = default);
}
