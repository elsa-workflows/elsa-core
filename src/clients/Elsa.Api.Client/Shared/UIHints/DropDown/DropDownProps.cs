namespace Elsa.Api.Client.Shared.UIHints.DropDown;

/// <summary>
/// Provides properties for the <c>dropdown</c> UI hint.
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