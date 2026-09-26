using System.Collections.Generic;

namespace Elsa.Studio.Labels.Models;

/// <summary>
/// Represents the response returned when listing workflow definition labels.
/// </summary>
public class WorkflowDefinitionLabelsListResponse
{
    /// <summary>
    /// Gets or sets the collection of label identifiers associated with the workflow definition.
    /// </summary>
    public ICollection<string> Items { get; set; } = new List<string>();
}

/// <summary>
/// Represents the request payload for updating workflow definition labels.
/// </summary>
public class WorkflowDefinitionLabelsUpdateRequest
{
    /// <summary>
    /// Gets or sets the identifier of the workflow definition to update.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the identifiers of the labels to associate with the workflow definition.
    /// </summary>
    public ICollection<string> LabelIds { get; set; } = new List<string>();
}

/// <summary>
/// Represents the response returned after updating workflow definition labels.
/// </summary>
public class WorkflowDefinitionLabelsUpdateResponse
{
    /// <summary>
    /// Gets or sets the identifiers of the labels associated with the workflow definition after the update.
    /// </summary>
    public ICollection<string> LabelIds { get; set; } = new List<string>();
}

