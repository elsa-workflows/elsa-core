using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Contracts;

public interface IAgentFactory
{
    Agent Create();
}

public class WorkflowAgentFactory : IAgentFactory
{
    public Agent Create()
    {
        throw new NotImplementedException();
    }
}