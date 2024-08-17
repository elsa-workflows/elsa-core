using Microsoft.SemanticKernel;

namespace Elsa.Agents;

public class Agent(AgentConfig agentConfig, Kernel kernel, AgentInvoker agentInvoker)
{
    public AgentConfig AgentConfig { get; } = agentConfig;
    public string Name => AgentConfig.Name;
    public async Task<InvokeAgentResult> ExecuteAsync(IDictionary<string, object?> inputs, CancellationToken cancellationToken = default)
    {
        return await agentInvoker.InvokeAgentAsync(kernel, Name, inputs, cancellationToken);
    }
}