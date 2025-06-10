namespace Elsa.Workflows.UIHints.CheckList;

/// <summary>
/// Provides properties for the <see cref="InputUIHints.CheckList"/> UI hint.
/// </summary>
public class CheckList
{
    /// <summary>
    /// The radio list.
    /// </summary>
    public IEnumerable<CheckListItem> Items { get; set; }

    /// <summary>
    /// The name of the provider that will provide the select list.
    /// </summary>
    public bool IsFlagsEnum { get; set; }
}