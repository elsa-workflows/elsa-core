using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.Contracts;

public interface IAiContextProvider
{
    string Kind { get; }
    ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default);
}
