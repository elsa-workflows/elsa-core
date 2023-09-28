using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;

namespace Elsa.Alterations.Core.Abstractions;

public abstract class AlterationBase : IAlteration
{
    public string Id { get; set; } = default!;
    public abstract ValueTask ApplyAsync(AlterationContext context, CancellationToken cancellationToken = default);
}