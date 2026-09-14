using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Resilience.Permissions;

/// <summary>
/// Stable resource names for Resilience. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class ResiliencePermissions
{
    /// <summary>Inspect retry attempt records.</summary>
    public const string Retries = "resilience/retries";
    /// <summary>Browse available resilience strategies.</summary>
    public const string Strategies = "resilience/strategies";
    /// <summary>Simulate a resilience response.</summary>
    public const string Simulation = "resilience/simulation";
}

/// <summary>Contributes the Resilience resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class ResiliencePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(ResiliencePermissions.Retries, [CoreVerbs.View], "Retry attempts", "Inspect retry attempt records.", "Resilience"),
        new(ResiliencePermissions.Strategies, [CoreVerbs.View], "Resilience strategies", "Browse available resilience strategies.", "Resilience"),
        new(ResiliencePermissions.Simulation, [CoreVerbs.Execute], "Resilience simulation", "Simulate a resilience response.", "Resilience"),
    ];
}
