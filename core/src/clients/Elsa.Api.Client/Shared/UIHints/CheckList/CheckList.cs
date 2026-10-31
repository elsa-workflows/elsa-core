namespace Elsa.Api.Client.Shared.UIHints.CheckList;

/// <summary>
/// Represents a list of check list items.
/// </summary>
/// <param name="Items">The items.</param>
/// <param name="IsFlagsEnum">Whether the select list represents a flags enum.</param>
public record CheckList(IEnumerable<CheckListItem> Items, bool IsFlagsEnum = false);