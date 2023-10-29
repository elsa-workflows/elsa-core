namespace Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;

/// <summary>
/// Represents a workflow context provider descriptor.
/// </summary>
/// <param name="Name">The name of the workflow context provider.</param>
/// <param name="Type">The type of the workflow context provider.</param>
public record WorkflowContextProviderDescriptor(string Name, string Type);