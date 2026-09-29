using System;

namespace Elsa.Studio.Labels.Models;

/// <summary>
/// Represents a label that can be associated with workflows.
/// </summary>
public class Label
{
    /// <summary>
    /// Gets or sets the unique identifier of the label.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the tenant identifier associated with the label, if any.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the label.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the normalized name of the label.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the label.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the color assigned to the label.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets when the label was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the label was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

