using Elsa.Studio.Authentication.Abstractions.Contracts;

namespace Elsa.Studio.Authentication.UI.Services;

public sealed class LoginMethodComponentRegistry : ILoginMethodComponentRegistry
{
    private readonly IReadOnlyDictionary<string, ILoginMethodComponentProvider> _providers;

    public LoginMethodComponentRegistry(IEnumerable<ILoginMethodComponentProvider> providers)
    {
        var materialized = providers.ToArray();
        foreach (var provider in materialized)
        {
            if (string.IsNullOrWhiteSpace(provider.Kind) ||
                !typeof(ILoginMethodComponent).IsAssignableFrom(provider.ComponentType))
                throw new InvalidOperationException("Login method component providers require a kind and an ILoginMethodComponent component type.");
        }

        var duplicate = materialized
            .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Login method kind '{duplicate.Key}' is registered more than once.");

        _providers = materialized.ToDictionary(x => x.Kind, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryResolve(string kind, out ILoginMethodComponentProvider provider) =>
        _providers.TryGetValue(kind, out provider!);
}
