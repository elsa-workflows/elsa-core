using Elsa.Alterations.Core.Contexts;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Represents a change to a given type <c>T</c>.
/// </summary>
public interface IAlterationHandler
{
    /// <summary>
    /// Returns <c>true</c> if the alteration is supported by this handler; otherwise, <c>false</c>.
    /// </summary>
    /// <param name="alteration"></param>
    bool CanHandle(IAlteration alteration);
    
    /// <summary>
    /// Applies the alteration to the specified context.
    /// </summary>
    /// <param name="context">A context object that contains information for the alteration and provides a way to alter the workflow instance and control the alteration process.</param>
    ValueTask HandleAsync(AlterationContext context);
}