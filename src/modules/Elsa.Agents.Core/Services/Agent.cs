using Microsoft.SemanticKernel;

namespace Elsa.Agents;

public class Agent(AgentConfig agentConfig, Kernel kernel, SkillExecutor skillExecutor)
{
    public AgentConfig AgentConfig { get; } = agentConfig;
    public string Name => agentConfig.Name;
    public async Task<ExecuteFunctionResult> ExecuteAsync(string skillName, string functionName, IDictionary<string, object?> inputs, CancellationToken cancellationToken = default)
    {
        return await skillExecutor.ExecuteFunctionAsync(kernel, skillName, functionName, inputs, cancellationToken);
    }
}