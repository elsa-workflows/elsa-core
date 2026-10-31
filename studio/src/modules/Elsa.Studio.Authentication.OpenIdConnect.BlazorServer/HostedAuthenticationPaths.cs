namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer;

/// <summary>
/// Builds hosted authentication URLs that keep a PathBase such as <c>/studio/</c>.
/// </summary>
public static class HostedAuthenticationPaths
{
    /// <summary>The form action for the Blazor Server OpenID Connect logout POST.</summary>
    public static string LogoutFormAction(string baseUri) =>
        new Uri(new Uri(EnsureTrailingSlash(baseUri)), "authentication/logout").AbsolutePath;

    private static string EnsureTrailingSlash(string baseUri) =>
        baseUri.EndsWith('/') ? baseUri : baseUri + "/";
}
