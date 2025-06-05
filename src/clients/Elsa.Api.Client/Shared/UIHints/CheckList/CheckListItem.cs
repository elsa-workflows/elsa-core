namespace Elsa.Api.Client.Shared.UIHints.CheckList;

/// <summary>
/// Represents an item in a <see cref="CheckList"/>.
/// </summary>
public record CheckListItem(string Text, string Value, bool IsChecked);