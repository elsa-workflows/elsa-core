namespace Elsa.Workflows.UIHints.Dropdown;

/// <summary>
/// Represents a list of select list items.
/// </summary>
/// <param name="Items">The items.</param>
public record SelectList(ICollection<SelectListItem> Items);