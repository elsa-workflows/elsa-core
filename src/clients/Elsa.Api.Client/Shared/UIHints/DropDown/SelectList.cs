namespace Elsa.Api.Client.Shared.UIHints.DropDown;

/// <summary>
/// Represents a list of select list items.
/// </summary>
/// <param name="Items">The items.</param>
/// <param name="IsFlagsEnum">Whether the select list represents a flags enum.</param>
public record SelectList(ICollection<SelectListItem> Items, bool IsFlagsEnum = false);