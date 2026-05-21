using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class AiToolRegistry(IEnumerable<IAiTool> tools, AiToolEnablementService enablementService) : IAiToolRegistry
{
    private readonly IReadOnlyCollection<IAiTool> _tools = tools.ToList();

    public ValueTask<IReadOnlyCollection<AiToolDefinition>> ListAsync(AiToolQuery query, CancellationToken cancellationToken = default)
    {
        var definitions = _tools
            .Select(x => x.Definition)
            .Where(x => query.Mutability == null || x.Mutability == query.Mutability)
            .Where(x => query.DangerLevel == null || x.DangerLevel == query.DangerLevel)
            .Where(x => query.Agent == null || x.AgentScopes.Count == 0 || x.AgentScopes.Contains(query.Agent, StringComparer.OrdinalIgnoreCase))
            .Select(x => x with { IsEnabled = enablementService.IsEnabled(x) })
            .ToList();

        return ValueTask.FromResult<IReadOnlyCollection<AiToolDefinition>>(definitions);
    }

    public ValueTask<IAiTool?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        var tool = _tools.FirstOrDefault(x => string.Equals(x.Definition.Name, name, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(tool);
    }
}
