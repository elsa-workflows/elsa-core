namespace Elsa.Api.Client.Shared.UIHints.RadioList;

/// <summary>
/// Represents a list of radio list items.
/// </summary>
/// <param name="Items">The items.</param>
/// <param name="IsFlagsEnum">Whether the select list represents a flags enum.</param>
public record RadioList(IEnumerable<RadioListItem> Items, bool IsFlagsEnum = false);