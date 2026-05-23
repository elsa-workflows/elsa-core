using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Services;

public class AiToolRegistry(IServiceScopeFactory scopeFactory, AiToolEnablementService enablementService) : IAiToolRegistry
{
    private readonly object _definitionCacheLock = new();
    private readonly ConcurrentDictionary<string, Type> _toolTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _uncacheableToolNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyCollection<AiToolDefinition>? _definitions;

    public ValueTask<IReadOnlyCollection<AiToolDefinition>> ListAsync(AiToolQuery query, CancellationToken cancellationToken = default)
    {
        var definitions = GetCachedDefinitions()
            .Where(x => IsVisible(x, query))
            .Select(x => x with { IsEnabled = enablementService.IsEnabled(x) })
            .ToList();

        return ValueTask.FromResult<IReadOnlyCollection<AiToolDefinition>>(definitions);
    }

    public ValueTask<IAiTool?> FindAsync(string name, AiToolQuery query, CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateScope();
        try
        {
            var tool = ResolveCachedTool(scope.ServiceProvider, name) ?? ResolveAndCacheTool(scope.ServiceProvider, name);
            if (tool == null || !IsVisible(tool.Definition, query) || !enablementService.IsEnabled(tool.Definition))
            {
                scope.Dispose();
                return ValueTask.FromResult<IAiTool?>(null);
            }

            return ValueTask.FromResult<IAiTool?>(new ScopedAiTool(scope, tool));
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private IAiTool? ResolveCachedTool(IServiceProvider serviceProvider, string name)
    {
        if (!_toolTypes.TryGetValue(name, out var toolType))
            return null;

        IAiTool tool;
        try
        {
            if (ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, toolType) is not IAiTool resolvedTool)
                return null;

            tool = resolvedTool;
        }
        catch (InvalidOperationException)
        {
            _uncacheableToolNames[name] = true;
            _toolTypes.TryRemove(name, out _);
            return null;
        }

        return string.Equals(tool.Definition.Name, name, StringComparison.OrdinalIgnoreCase) ? tool : null;
    }

    private IAiTool? ResolveAndCacheTool(IServiceProvider serviceProvider, string name)
    {
        var tools = serviceProvider.GetServices<IAiTool>().ToList();
        UpdateToolTypeCache(tools);
        return tools.FirstOrDefault(x => string.Equals(x.Definition.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyCollection<AiToolDefinition> GetCachedDefinitions()
    {
        if (_definitions != null)
            return _definitions;

        lock (_definitionCacheLock)
        {
            if (_definitions != null)
                return _definitions;

            using var scope = scopeFactory.CreateScope();
            var tools = scope.ServiceProvider.GetServices<IAiTool>().ToList();
            UpdateToolTypeCache(tools);
            _definitions = tools.Select(x => x.Definition).ToList();
            return _definitions;
        }
    }

    private void UpdateToolTypeCache(IReadOnlyCollection<IAiTool> tools)
    {
        var namesByType = tools
            .GroupBy(x => x.GetType())
            .ToDictionary(x => x.Key, x => x.Select(tool => tool.Definition.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

        foreach (var (toolType, toolNames) in namesByType)
        {
            if (toolNames.Count != 1)
            {
                foreach (var toolName in toolNames)
                {
                    _uncacheableToolNames[toolName] = true;
                    _toolTypes.TryRemove(toolName, out _);
                }

                continue;
            }

            var name = toolNames[0];
            if (_uncacheableToolNames.ContainsKey(name))
                continue;

            _toolTypes[name] = toolType;
        }
    }

    private static bool IsVisible(AiToolDefinition definition, AiToolQuery query) =>
        (query.Mutability == null || definition.Mutability == query.Mutability) &&
        (query.DangerLevel == null || definition.DangerLevel == query.DangerLevel) &&
        IsVisibleForAgent(definition, query.Agent) &&
        IsVisibleForTenant(definition, query.TenantId) &&
        IsVisibleForActor(definition, query.ActorId) &&
        HasRequiredPermissions(definition, query.UserPermissions);

    private static bool IsVisibleForAgent(AiToolDefinition definition, string? agent)
    {
        if (definition.AgentScopes.Count == 0)
            return true;

        return !string.IsNullOrWhiteSpace(agent) &&
               definition.AgentScopes.Contains(agent, StringComparer.OrdinalIgnoreCase);
    }

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
