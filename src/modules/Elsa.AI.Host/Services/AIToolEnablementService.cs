using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class AIToolEnablementService
{
    private readonly ConcurrentDictionary<string, byte> _enabledToolNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _enabledAdministrativeToolNames = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled(AIToolDefinition definition)
    {
        if (definition.Mutability == AIToolMutability.Administrative)
            return _enabledAdministrativeToolNames.ContainsKey(definition.Name);

        if (definition.EnabledByDefault)
            return true;

        return _enabledToolNames.ContainsKey(definition.Name);
    }

    /// <summary>
    /// Enables a non-administrative tool by name. Administrative tools remain disabled by this service and must be exposed through a dedicated governed path.
    /// </summary>
    public void Enable(string toolName)
    {
        _enabledToolNames.TryAdd(toolName, 0);
    }

    /// <summary>
    /// Enables an administrative tool by name. Callers must wrap this in their own governed approval and audit path.
    /// </summary>
    public void EnableAdministrative(string toolName)
    {
        _enabledAdministrativeToolNames.TryAdd(toolName, 0);
    }

    public void Disable(string toolName)
    {
        _enabledToolNames.TryRemove(toolName, out _);
        _enabledAdministrativeToolNames.TryRemove(toolName, out _);
    }
}
