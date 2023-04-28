namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// A factory of workflow builders.
/// </summary>
public interface IWorkflowBuilderFactory
{
    /// <summary>
    /// Creates a new workflow builder.
    /// </summary>
    /// <returns>A new workflow builder.</returns>
    IWorkflowBuilder CreateBuilder();
}