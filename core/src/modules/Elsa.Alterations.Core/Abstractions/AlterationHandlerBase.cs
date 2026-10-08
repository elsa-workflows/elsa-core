using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;

namespace Elsa.Alterations.Core.Abstractions;

/// <summary>
/// A base class for alterations.
/// </summary>
public abstract class AlterationHandlerBase : IAlterationHandler
{
    /// <inheritdoc />
    public abstract bool CanHandle(IAlteration alteration);

    /// <inheritdoc />
    public abstract ValueTask HandleAsync(AlterationContext context);
}

/// <inheritdoc />
public abstract class AlterationHandlerBase<T> : AlterationHandlerBase where T : IAlteration
{
    /// <inheritdoc />
    public override bool CanHandle(IAlteration alteration) => alteration is T;

    /// <inheritdoc />
    public override ValueTask HandleAsync(AlterationContext context) => HandleAsync(context, (T)context.Alteration);

    /// <summary>
    /// Applies the alteration to the specified context.
    /// </summary>
    /// <param name="context">A context object that contains information for the alteration and provides a way to alter the workflow instance and control the alteration process.</param>
    /// <param name="alteration">A strongly typed alteration.</param>
    protected abstract ValueTask HandleAsync(AlterationContext context, T alteration);
}