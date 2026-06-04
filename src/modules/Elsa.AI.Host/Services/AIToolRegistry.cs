using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.Services;

public class AIToolRegistry(IServiceScopeFactory scopeFactory, AIToolEnablementService enablementService) : IAIToolRegistry
{
    private readonly object _definitionCacheLock = new();
    private readonly ConcurrentDictionary<string, Type> _toolTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _uncacheableToolNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, bool> _uncacheableToolTypes = new();
    private volatile IReadOnlyCollection<AIToolDefinition>? _definitions;

    public ValueTask<IReadOnlyCollection<AIToolDefinition>> ListAsync(AIToolQuery query, CancellationToken cancellationToken = default)
    {
        var definitions = GetCachedDefinitions()
            .Where(x => IsVisible(x, query))
            .Select(x => x with { IsEnabled = enablementService.IsEnabled(x) })
            .ToList();

        return ValueTask.FromResult<IReadOnlyCollection<AIToolDefinition>>(definitions);
    }

    public ValueTask<IAITool?> FindAsync(string name, AIToolQuery query, CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateScope();
        ResolvedTool? resolvedTool = null;
        try
        {
            resolvedTool = ResolveCachedTool(scope.ServiceProvider, name) ?? ResolveAndCacheTool(scope.ServiceProvider, name);
            if (resolvedTool == null || !TryGetDefinition(resolvedTool.Tool, out var definition) || !IsVisible(definition, query) || !enablementService.IsEnabled(definition))
            {
                resolvedTool?.Dispose();
                scope.Dispose();
                return ValueTask.FromResult<IAITool?>(null);
            }

            return ValueTask.FromResult<IAITool?>(new ScopedAITool(scope, resolvedTool.Tool, resolvedTool.DisposeTool));
        }
        catch
        {
            resolvedTool?.Dispose();
            scope.Dispose();
            throw;
        }
    }

    private ResolvedTool? ResolveCachedTool(IServiceProvider serviceProvider, string name)
    {
        if (!_toolTypes.TryGetValue(name, out var toolType))
            return null;

        ResolvedTool resolvedTool;
        try
        {
            if (serviceProvider.GetService(toolType) is IAITool scopeOwnedTool)
                resolvedTool = new ResolvedTool(scopeOwnedTool, DisposeTool: false);
            else if (ActivatorUtilities.CreateInstance(serviceProvider, toolType) is IAITool createdTool)
                resolvedTool = new ResolvedTool(createdTool, DisposeTool: true);
            else
                return null;
        }
        catch (InvalidOperationException)
        {
            _uncacheableToolNames[name] = true;
            _toolTypes.TryRemove(name, out _);
            return null;
        }

        if (!TryGetDefinition(resolvedTool.Tool, out var definition))
        {
            resolvedTool.Dispose();
            _uncacheableToolNames[name] = true;
            _toolTypes.TryRemove(name, out _);
            return null;
        }

        if (string.Equals(definition.Name, name, StringComparison.OrdinalIgnoreCase))
            return resolvedTool;

        resolvedTool.Dispose();
        return null;
    }

    private ResolvedTool? ResolveAndCacheTool(IServiceProvider serviceProvider, string name)
    {
        var tools = serviceProvider.GetServices<IAITool>().ToList();
        var toolInfos = GetToolInfos(tools);
        UpdateToolTypeCache(toolInfos);
        var tool = toolInfos.FirstOrDefault(x => string.Equals(x.Definition.Name, name, StringComparison.OrdinalIgnoreCase))?.Tool;
        return tool == null ? null : new ResolvedTool(tool, DisposeTool: false);
    }

    private IReadOnlyCollection<AIToolDefinition> GetCachedDefinitions()
    {
        if (_definitions != null)
            return _definitions;

        lock (_definitionCacheLock)
        {
            if (_definitions != null)
                return _definitions;

            using var scope = scopeFactory.CreateScope();
            var tools = scope.ServiceProvider.GetServices<IAITool>().ToList();
            var toolInfos = GetToolInfos(tools);
            UpdateToolTypeCache(toolInfos);
            // Definitions are cached by design. Runtime enablement is refreshed in ListAsync; dynamic Definition properties are not.
            _definitions = toolInfos.Select(x => x.Definition).ToList().AsReadOnly();
            return _definitions;
        }
    }

    private void UpdateToolTypeCache(IReadOnlyCollection<ToolInfo> toolInfos)
    {
        var namesByType = toolInfos
            .Where(x => !_uncacheableToolTypes.ContainsKey(x.ToolType))
            .Where(x => !string.IsNullOrWhiteSpace(x.Definition.Name))
            .GroupBy(x => x.ToolType)
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

    private IReadOnlyCollection<ToolInfo> GetToolInfos(IReadOnlyCollection<IAITool> tools)
    {
        return tools
            .Select(CreateToolInfo)
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();
    }

    private ToolInfo? CreateToolInfo(IAITool tool) =>
        TryGetDefinition(tool, out var definition)
            ? new ToolInfo(tool.GetType(), tool, definition)
            : null;

    private bool TryGetDefinition(IAITool tool, out AIToolDefinition definition)
    {
        try
        {
            definition = tool.Definition;
            return true;
        }
        catch (InvalidOperationException)
        {
            _uncacheableToolTypes[tool.GetType()] = true;
            definition = default!;
            return false;
        }
    }

    private static bool IsVisible(AIToolDefinition definition, AIToolQuery query) =>
        (query.Mutability == null || definition.Mutability == query.Mutability) &&
        (query.DangerLevel == null || definition.DangerLevel == query.DangerLevel) &&
        IsVisibleForAgent(definition, query.Agent) &&
        IsVisibleForTenant(definition, query.TenantId) &&
        IsVisibleForActor(definition, query.ActorId) &&
        HasRequiredPermissions(definition, query.UserPermissions);

    private static bool IsVisibleForAgent(AIToolDefinition definition, string? agent)
    {
        if (definition.AgentScopes.Count == 0)
            return true;

        return !string.IsNullOrWhiteSpace(agent) &&
               definition.AgentScopes.Contains(agent, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsVisibleForTenant(AIToolDefinition definition, string? tenantId)
    {
        if (tenantId != null)
        {
            if (definition.TenantBehavior == AITenantBehavior.HostScoped)
                return false;

            if (definition.TenantBehavior == AITenantBehavior.CrossTenantDenied)
                return definition.TenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase);

            return definition.TenantIds.Count == 0 || definition.TenantIds.Contains(tenantId, StringComparer.OrdinalIgnoreCase);
        }

        return definition.TenantBehavior != AITenantBehavior.CrossTenantDenied && definition.TenantIds.Count == 0;
    }

    private static bool IsVisibleForActor(AIToolDefinition definition, string? actorId)
    {
        return definition.ActorIds.Count == 0 ||
               (!string.IsNullOrWhiteSpace(actorId) && definition.ActorIds.Contains(actorId, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasRequiredPermissions(AIToolDefinition definition, ICollection<string> userPermissions)
    {
        if (definition.Permissions.Count == 0)
            return true;

        var grantedPermissions = userPermissions
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return grantedPermissions.Contains(PermissionNames.All) || definition.Permissions.All(grantedPermissions.Contains);
    }

    private class ScopedAITool(IServiceScope scope, IAITool inner, bool disposeInner) : IAITool
    {
        private bool _disposed;

        public AIToolDefinition Definition => inner.Definition;

        public async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
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
            if (disposeInner)
                inner.Dispose();

            scope.Dispose();
        }
    }

    private record ResolvedTool(IAITool Tool, bool DisposeTool)
    {
        public void Dispose()
        {
            if (DisposeTool)
                Tool.Dispose();
        }
    }

    private record ToolInfo(Type ToolType, IAITool Tool, AIToolDefinition Definition);
}
