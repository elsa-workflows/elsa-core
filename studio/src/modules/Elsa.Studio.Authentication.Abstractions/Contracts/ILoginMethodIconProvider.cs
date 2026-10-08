using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>Contributes trusted, locally compiled icon registrations.</summary>
public interface ILoginMethodIconProvider
{
    IReadOnlyCollection<LoginMethodIconRegistration> GetIcons();
}

public interface ILoginMethodIconRegistry
{
    LoginMethodIcon Resolve(string? iconId);
}
