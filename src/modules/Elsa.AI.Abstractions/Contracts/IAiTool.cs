using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAiTool
{
    AiToolDefinition Definition { get; }
    ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IAiToolRegistry
{
    ValueTask<IReadOnlyCollection<AiToolDefinition>> ListAsync(AiToolQuery query, CancellationToken cancellationToken = default);
    ValueTask<IAiTool?> FindAsync(string name, AiToolQuery query, CancellationToken cancellationToken = default);
    ValueTask<IAiTool?> FindAsync(string name, CancellationToken cancellationToken = default);
}
