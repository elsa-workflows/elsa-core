using System.Collections.Concurrent;
using Elsa.Copilot.Contracts;
using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Implementations;

public class AgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, KernelAgent> _agents = new();

    public void Register(KernelAgent agent) => _agents[agent.Name] = agent;

    public IEnumerable<KernelAgent> List() => _agents.Values;

    public KernelAgent? GetByName(string name) => _agents.TryGetValue(name, out var agent) ? agent : null;
}