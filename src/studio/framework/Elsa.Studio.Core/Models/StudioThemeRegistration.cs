namespace Elsa.Studio.Models;

/// <summary>
/// Describes a registered Elsa Studio theme pack.
/// </summary>
public sealed record StudioThemeRegistration(string Id, Type ProviderType);
