using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class AiToolEnablementService
{
    private readonly ConcurrentDictionary<string, byte> _enabledToolNames = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled(AiToolDefinition definition)
    {
        if (definition.Mutability == AiToolMutability.Administrative)
            return false;

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

    public void Disable(string toolName)
    {
        _enabledToolNames.TryRemove(toolName, out _);
    }
}
