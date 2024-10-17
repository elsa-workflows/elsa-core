namespace Elsa.Workflows.UIHints.SingleLine;

/// <summary>
/// Provides additional options for the SingleLine input field.
/// </summary>
public class SingleLineProps
{
    /// <summary>
    /// Gets or sets adornment text.
    /// </summary>
    public string? AdornmentText { get; set; }

    /// <summary>
    /// Gets or sets enabling or disabling the copy adornment.
    /// </summary>
    public bool EnableCopyAdornment { get; set; } = false;
}