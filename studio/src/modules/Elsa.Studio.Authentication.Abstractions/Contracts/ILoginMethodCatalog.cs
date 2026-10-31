using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>Discovers the login methods contributed by one active authentication integration.</summary>
public interface ILoginMethodCatalog
{
    ValueTask<LoginMethodCatalogResult> ListAsync(CancellationToken cancellationToken = default);
}
