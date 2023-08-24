using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.Scripting.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;

namespace Elsa.Api.Client.Contracts;

/// <summary>
/// Represents a client for the Elsa API. Each API is exposed as a property.
/// </summary>
public interface IElsaClient
{
    /// <summary>
    /// Gets the workflow definitions API.
    /// </summary>
    IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    
    /// <summary>
    /// Gets the activity descriptors API.
    /// </summary>
    IActivityDescriptorsApi ActivityDescriptors { get; }
    
    /// <summary>
    /// Gets the workflow instances API.
    /// </summary>
    IWorkflowInstancesApi WorkflowInstances { get; }

    /// <summary>
    /// Gets the javascript API.
    /// </summary>
    IJavaScriptApi JavaScript { get; }
}