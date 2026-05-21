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
            .Where(x => IsVisibleForTenant(x, query.TenantId))
            .Where(x => IsVisibleForActor(x, query.ActorId))
            .Select(x => x with { IsEnabled = enablementService.IsEnabled(x) })
            .ToList();

        return ValueTask.FromResult<IReadOnlyCollection<AiToolDefinition>>(definitions);
    }

    public ValueTask<IAiTool?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        var tool = _tools.FirstOrDefault(x => string.Equals(x.Definition.Name, name, StringComparison.OrdinalIgnoreCase));
        return ValueTask.FromResult(tool);
    }

    private static bool IsVisibleForTenant(AiToolDefinition definition, string? tenantId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            if (definition.TenantBehavior is AiTenantBehavior.HostScoped or AiTenantBehavior.CrossTenantDenied)
                return false;

            return definition.TenantIds.Count == 0 || definition.TenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase);
        }

        return definition.TenantIds.Count == 0;
    }

    private static bool IsVisibleForActor(AiToolDefinition definition, string? actorId)
    {
        return definition.ActorIds.Count == 0 ||
               (!string.IsNullOrWhiteSpace(actorId) && definition.ActorIds.Contains(actorId, StringComparer.OrdinalIgnoreCase));
    }
}
