namespace Elsa.Alterations.Core.Options;

/// <summary>
/// Options for the Alteration module.
/// </summary>
public class AlterationOptions
{
    /// <summary>
    /// The types of alterations that are supported.
    /// </summary>
    public ISet<Type> AlterationTypes { get; set; } = new HashSet<Type>();
}