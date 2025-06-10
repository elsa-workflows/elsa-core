namespace Elsa.Api.Client.Shared.UIHints.CheckList;

/// <summary>
/// Represents an item in a <see cref="CheckList"/>.
/// </summary>
public class CheckListItem
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string Text { get; set; }
    public string Value { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public bool IsChecked { get; set; }
}