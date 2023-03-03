namespace Elsa.Workflows.Core.Contracts;

public interface IWorkflowBuilderFactory
{
    IWorkflowBuilder CreateBuilder();
}