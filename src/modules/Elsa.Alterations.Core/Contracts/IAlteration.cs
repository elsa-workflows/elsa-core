using Elsa.Alterations.Core.Contexts;

namespace Elsa.Alterations.Core.Contracts;

/// <summary>
/// Represents a change to a given type <c>T</c>.
/// </summary>
public interface IAlteration
{
    string Id { get; set; }
    ValueTask ApplyAsync(AlterationContext context, CancellationToken cancellationToken = default);
}