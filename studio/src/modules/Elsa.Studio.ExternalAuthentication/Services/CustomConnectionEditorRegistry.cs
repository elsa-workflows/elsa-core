using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>Registers optional provider-specific editor components without compromising the generic descriptor fallback.</summary>
public interface ICustomConnectionEditorRegistration
{
    string Key { get; }
    int ContractVersion { get; }
    Type ComponentType { get; }
}

/// <summary>
/// Marker for custom editors that accept the same public parameter contract as <c>ConnectionEditor</c>.
/// </summary>
public interface IConnectionCustomEditor : IComponent;

/// <summary>
/// Opts a custom connection editor into notifying its host when it has unsaved changes.
/// Implementations must expose <see cref="Changed"/> as a Blazor parameter.
/// </summary>
public interface IConnectionCustomEditorWithChangeTracking : IConnectionCustomEditor
{
    EventCallback Changed { get; set; }
}

public interface ICustomConnectionEditorRegistry
{
    bool TryResolve(CustomEditorContract? contract, out Type componentType);
}

public sealed class CustomConnectionEditorRegistry(IEnumerable<ICustomConnectionEditorRegistration> registrations) : ICustomConnectionEditorRegistry
{
    private readonly IReadOnlyDictionary<(string Key, int Version), Type> editors = CreateEditors(registrations);

    public bool TryResolve(CustomEditorContract? contract, out Type componentType)
    {
        if (contract is not null && editors.TryGetValue((contract.Key, contract.ContractVersion), out var registeredType))
        {
            componentType = registeredType;
            return true;
        }

        componentType = default!;
        return false;
    }

    private static IReadOnlyDictionary<(string Key, int Version), Type> CreateEditors(IEnumerable<ICustomConnectionEditorRegistration> registrations)
    {
        var validated = registrations.ToArray();
        foreach (var registration in validated)
        {
            if (string.IsNullOrWhiteSpace(registration.Key) || registration.ContractVersion <= 0 || !typeof(IConnectionCustomEditor).IsAssignableFrom(registration.ComponentType))
                throw new InvalidOperationException("Custom connection editor registrations require a non-empty key, positive contract version, and an IConnectionCustomEditor component type.");

        }

        var duplicates = validated.GroupBy(registration => (registration.Key, registration.ContractVersion)).FirstOrDefault(group => group.Count() > 1);
        if (duplicates is not null)
            throw new InvalidOperationException($"The custom connection editor contract '{duplicates.Key.Key}' version '{duplicates.Key.ContractVersion}' is registered more than once.");

        return validated.ToDictionary(registration => (registration.Key, registration.ContractVersion), registration => registration.ComponentType);
    }
}

public static class CustomConnectionEditorServiceCollectionExtensions
{
    /// <summary>Registers a provider-specific editor for one exact server custom-editor contract.</summary>
    public static IServiceCollection AddExternalAuthenticationCustomEditor<TComponent>(this IServiceCollection services, string key, int contractVersion)
        where TComponent : ComponentBase, IConnectionCustomEditor
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (contractVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(contractVersion));

        services.AddTransient<TComponent>();
        services.AddSingleton<ICustomConnectionEditorRegistration>(new CustomConnectionEditorRegistration(key, contractVersion, typeof(TComponent)));
        return services;
    }

    private sealed class CustomConnectionEditorRegistration(string key, int contractVersion, Type componentType) : ICustomConnectionEditorRegistration
    {
        public string Key => key;
        public int ContractVersion => contractVersion;
        public Type ComponentType => componentType;
    }
}
