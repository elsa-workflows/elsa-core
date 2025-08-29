namespace Elsa.Workflows.UIHints.Dictionary;

/// <summary>
/// Provides properties for the <see cref="InputUIHints.Dictionary"/> UI hint.
/// </summary>
public class Dictionary
{
    /// <summary>
    /// The dictionary items.
    /// </summary>
    public IEnumerable<DictionaryItem> Items { get; set; } = [];
}
