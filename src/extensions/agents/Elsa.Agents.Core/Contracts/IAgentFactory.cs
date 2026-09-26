using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

/// <summary>
/// Factory for creating agents from agent configurations.
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Creates a ChatCompletionAgent from an Elsa agent configuration.
    /// </summary>
    ChatCompletionAgent CreateAgent(AgentConfig agentConfig);
}
