namespace Elsa.Studio.Authentication.UI.Models;

/// <summary>
/// Records a login presentation provider under a deployment-facing identifier.
/// </summary>
public sealed record LoginThemeRegistration(string Id, Type ProviderType);
