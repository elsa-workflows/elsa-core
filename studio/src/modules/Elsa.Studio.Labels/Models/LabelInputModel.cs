using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Labels.Models;

/// <summary>
/// The input model for a label.
/// </summary>
public class LabelInputModel
{
    /// <summary>
    /// Gets or sets the name of the label.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the label.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the color of the label.
    /// </summary>
    public string Color { get; set; } = string.Empty;
}
