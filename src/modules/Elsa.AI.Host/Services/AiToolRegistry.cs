using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Services;

public class AiToolRegistry(IServiceScopeFactory scopeFactory, AiToolEnablementService enablementService) : IAiToolRegistry
{
    public ValueTask<IReadOnlyCollection<AiToolDefinition>> ListAsync(AiToolQuery query, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var definitions = scope.ServiceProvider.GetServices<IAiTool>()
            .Select(x => x.Definition)
            .Where(x => IsVisible(x, query))
            .Select(x => x with { IsEnabled = enablementService.IsEnabled(x) })
            .ToList();

        return ValueTask.FromResult<IReadOnlyCollection<AiToolDefinition>>(definitions);
    }

    public ValueTask<IAiTool?> FindAsync(string name, AiToolQuery query, CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateScope();
        var tool = scope.ServiceProvider.GetServices<IAiTool>().FirstOrDefault(x => string.Equals(x.Definition.Name, name, StringComparison.OrdinalIgnoreCase));
        if (tool == null || !IsVisible(tool.Definition, query) || !enablementService.IsEnabled(tool.Definition))
        {
            scope.Dispose();
            tool = null;
        }
        else
        {
            tool = new ScopedAiTool(scope, tool);
        }

        return ValueTask.FromResult(tool);
    }

    private static bool IsVisible(AiToolDefinition definition, AiToolQuery query) =>
        (query.Mutability == null || definition.Mutability == query.Mutability) &&
        (query.DangerLevel == null || definition.DangerLevel == query.DangerLevel) &&
        (query.Agent == null || definition.AgentScopes.Count == 0 || definition.AgentScopes.Contains(query.Agent, StringComparer.OrdinalIgnoreCase)) &&
        IsVisibleForTenant(definition, query.TenantId) &&
        IsVisibleForActor(definition, query.ActorId) &&
        HasRequiredPermissions(definition, query.UserPermissions);

    private static bool IsVisibleForTenant(AiToolDefinition definition, string? tenantId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            if (definition.TenantBehavior == AiTenantBehavior.HostScoped)
                return false;

            if (definition.TenantBehavior == AiTenantBehavior.CrossTenantDenied)
                return definition.TenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase);

            return definition.TenantIds.Count == 0 || definition.TenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase);
        }

        return definition.TenantBehavior != AiTenantBehavior.CrossTenantDenied && definition.TenantIds.Count == 0;
    }

    private static bool IsVisibleForActor(AiToolDefinition definition, string? actorId)
    {
        return definition.ActorIds.Count == 0 ||
               (!string.IsNullOrWhiteSpace(actorId) && definition.ActorIds.Contains(actorId, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasRequiredPermissions(AiToolDefinition definition, ICollection<string> userPermissions)
    {
        if (definition.Permissions.Count == 0)
            return true;

        var grantedPermissions = userPermissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return grantedPermissions.Contains(PermissionNames.All) || definition.Permissions.All(grantedPermissions.Contains);
    }

    private class ScopedAiTool(IServiceScope scope, IAiTool inner) : IAiTool, IDisposable
    {
        private bool _disposed;

        public AiToolDefinition Definition => inner.Definition;

        public async ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await inner.ExecuteAsync(context, cancellationToken);
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            scope.Dispose();
        }
    }
}
