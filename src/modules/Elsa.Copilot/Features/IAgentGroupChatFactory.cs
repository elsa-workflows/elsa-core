using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Features;

public interface IAgentGroupChatFactory
{
    Task<AgentGroupChat> CreateAsync(CancellationToken cancellationToken = default);
}