using Elsa.Copilot.Contracts;
using Elsa.Copilot.Features;
using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0110

namespace Elsa.Copilot.Implementations;

public class AgentGroupChatFactory : IAgentGroupChatFactory
{
    private readonly IAgentRegistry _agentRegistry;

    public AgentGroupChatFactory(IAgentRegistry agentRegistry)
    {
        _agentRegistry = agentRegistry;
    }

    public Task<AgentGroupChat> CreateAsync(CancellationToken cancellationToken = default)
    {
        var chat = new AgentGroupChat();

        foreach (var agent in _agentRegistry.List())
            chat.AddAgent(agent);

        return Task.FromResult(chat);
    }
}