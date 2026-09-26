using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Labels.Models;

/// <summary>
/// Represents a descriptor for a workflow definition label.
/// </summary>
public class WorkflowDefinitionLabelDescriptor
{
    /// <summary>
    /// Gets or sets the unique identifier of the label.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the name of the label.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets the workflow definition ID associated with the label.
    /// </summary>
    public string WorkflowDefinitionId { get; internal set; } = default!;

    /// <summary>
    /// Gets the color associated with the label, if any.
    /// </summary>
    public string? Color { get; internal set; }
}
