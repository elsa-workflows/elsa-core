namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>
/// Records an authentication handler registration so startup validation can detect conflicting trust modes.
/// </summary>
public sealed record StudioAuthenticationProviderRegistration(StudioAuthenticationProvider Provider);
