using Elsa.Contracts;

namespace Elsa.Runtime.Contracts;

public interface IWorkflow
{
    void Build(IWorkflowDefinitionBuilder workflow);
}