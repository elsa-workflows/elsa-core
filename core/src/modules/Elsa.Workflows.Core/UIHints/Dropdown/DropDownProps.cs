namespace Elsa.Workflows.UIHints.Dropdown;

/// <summary>
/// Provides properties for the <see cref="InputUIHints.DropDown"/> UI hint.
/// </summary>
public class DropDownProps
{
    /// <summary>
    /// The select list.
    /// </summary>
    public SelectList? SelectList { get; set; }

    /// <summary>
    /// The name of the provider that will provide the select list.
    /// </summary>
    public string? ProviderName { get; set; }
}