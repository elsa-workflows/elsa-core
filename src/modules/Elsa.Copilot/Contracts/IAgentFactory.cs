using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Contracts;

public interface IAgentProducer
{
    string Name { get; }
    Task<Agent> ProduceAsync(CancellationToken cancellationToken = default);
}