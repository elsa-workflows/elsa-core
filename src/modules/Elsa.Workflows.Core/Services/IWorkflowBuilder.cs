using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A workflow builder collects information about a workflow to be built programmatically.
/// </summary>
public interface IWorkflowBuilder
{
    /// <summary>
    /// The definition ID to use for the workflow being built.
    /// </summary>
    string? DefinitionId { get; }
    
    /// <summary>
    /// The version ID to use for the workflow being built.
    /// </summary>
    string? Id { get; }
    
    /// <summary>
    /// The version of the workflow being built.
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// The name of the workflow.
    /// </summary>
    string? Name { get; }
    
    /// <summary>
    /// A description of the workflow.
    /// </summary>
    string? Description { get; }
    
    /// <summary>
    /// The activity to execute when the workflow is run. 
    /// </summary>
    IActivity? Root { get; set; }
    
    /// <summary>
    /// The workflow variables to store with the workflow being built.
    /// </summary>
    ICollection<Variable> Variables { get; set; }
    
    /// <summary>
    /// A set of properties that can be used for storing application-specific information about the workflow being built.
    /// </summary>
    IDictionary<string, object> CustomProperties { get; }
    
    /// <summary>
    /// A fluent method for assigning the <see cref="DefinitionId"/>.
    /// </summary>
    IWorkflowBuilder WithDefinitionId(string definitionId);
    
    /// <summary>
    /// A fluent method for assigning the <see cref="Id"/>.
    /// </summary>
    IWorkflowBuilder WithId(string id);
    
    /// <summary>
    /// A fluent method for assigning the <see cref="Version"/>.
    /// </summary>
    IWorkflowBuilder WithVersion(int version);
    
    /// <summary>
    /// A fluent method for assigning the <see cref="Root"/>.
    /// </summary>
    IWorkflowBuilder WithRoot(IActivity root);
    
    /// <summary>
    /// A fluent method for assigning the <see cref="Name"/>.
    /// </summary>
    IWorkflowBuilder WithName(string value);
    
    /// <summary>
    /// A fluent method for assigning the <see cref="Description"/>.
    /// </summary>
    IWorkflowBuilder WithDescription(string value);
    
    /// <summary>
    /// A fluent method for adding a variable to <see cref="Variables"/>.
    /// </summary>
    Variable<T> WithVariable<T>(string? storageDriverId = default);
    
    /// <summary>
    /// A fluent method for adding a variable to <see cref="Variables"/>.
    /// </summary>
    Variable<T> WithVariable<T>(string name, T value, string? storageDriverId = default);
    
    /// <summary>
    /// A fluent method for adding a variable to <see cref="Variables"/>.
    /// </summary>
    Variable<T> WithVariable<T>(T value, string? storageDriverId = default);
    
    /// <summary>
    /// A fluent method for adding a variable to <see cref="Variables"/>.
    /// </summary>
    IWorkflowBuilder WithVariable(Variable variable);
    
    /// <summary>
    /// A fluent method for adding a variable to <see cref="Variables"/>.
    /// </summary>
    IWorkflowBuilder WithVariables(params Variable[] variables);

    /// <summary>
    /// A fluent method for configuring <see cref="WorkflowOptions"/>.
    /// </summary>
    IWorkflowBuilder ConfigureOptions(Action<WorkflowOptions> configure);

    /// <summary>
    /// A fluent method for adding a property to <see cref="CustomProperties"/>.
    /// </summary>
    IWorkflowBuilder WithCustomProperty(string name, object value);
    
    /// <summary>
    /// Build a new <see cref="Workflow"/> instance using the information collected in this builder.
    /// </summary>
    Workflow BuildWorkflow();
    
    /// <summary>
    /// Creates a new <see cref="Workflow"/> instance using the specified <see cref="IWorkflow"/> definition.
    /// </summary>
    Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default);
}