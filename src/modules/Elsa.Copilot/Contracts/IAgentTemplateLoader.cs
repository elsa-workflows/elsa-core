using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Contracts;

public interface IAgentTemplateLoader
{
    Task<IEnumerable<KernelAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default);
}