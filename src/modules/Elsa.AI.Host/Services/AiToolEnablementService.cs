using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class AiToolEnablementService
{
    private readonly HashSet<string> _enabledToolNames = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled(AiToolDefinition definition)
    {
        if (definition.Mutability == AiToolMutability.Administrative)
            return false;

        if (definition.Mutability == AiToolMutability.ReadOnly && definition.EnabledByDefault)
            return true;

        return _enabledToolNames.Contains(definition.Name);
    }

    public void Enable(string toolName)
    {
        _enabledToolNames.Add(toolName);
    }

    public void Disable(string toolName)
    {
        _enabledToolNames.Remove(toolName);
    }
}
