namespace Elsa.Studio.Security.Models;

/// <summary>
/// An in-place edit of an advanced grant. The grant card writes the draft here as the user types, so saving the role
/// can apply an edit the user never confirmed without re-rendering the whole editor on every keystroke.
/// </summary>
public sealed class AdvancedGrantEdit(string original)
{
    /// <summary>The stored grant being edited.</summary>
    public string Original { get; } = original;

    /// <summary>The value currently in the edit field.</summary>
    public string Draft { get; set; } = original;

    /// <summary>The validation error for <see cref="Draft"/>, if the last attempt to apply it failed.</summary>
    public string? Error { get; set; }
}
