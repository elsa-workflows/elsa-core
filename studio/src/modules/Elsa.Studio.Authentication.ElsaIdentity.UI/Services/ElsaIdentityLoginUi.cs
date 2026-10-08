using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using MudBlazor;

namespace Elsa.Studio.Authentication.ElsaIdentity.UI.Services;

public sealed class ElsaIdentityLoginMethodCatalog : ILoginMethodCatalog
{
    public ValueTask<LoginMethodCatalogResult> ListAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new LoginMethodCatalogResult(
        [
            new LoginMethodDescriptor(
                "elsa-identity",
                "elsa-identity",
                "elsa-identity",
                "Elsa account",
                "elsa",
                0,
                true,
                string.Empty)
        ],
        "elsa-identity"));
}

public sealed class ElsaIdentityLoginMethodComponentProvider : ILoginMethodComponentProvider
{
    public string Kind => "elsa-identity";
    public Type ComponentType => typeof(ElsaIdentityLoginMethod);
}

public sealed class ElsaIdentityLoginMethodIconProvider : ILoginMethodIconProvider
{
    public IReadOnlyCollection<LoginMethodIconRegistration> GetIcons() =>
    [
        new("elsa", new(Icons.Material.Outlined.AccountCircle, "Elsa"))
    ];
}
