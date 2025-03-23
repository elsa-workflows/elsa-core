using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Contracts;

public interface IAgentRegistry
{
    void Register(KernelAgent agent);
    IEnumerable<KernelAgent> List();
    KernelAgent? GetByName(string name);
}