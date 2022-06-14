namespace Elsa.Workflows.Core.Services;

public interface IWorkflowDefinitionBuilderFactory
{
    IWorkflowDefinitionBuilder CreateBuilder();
}