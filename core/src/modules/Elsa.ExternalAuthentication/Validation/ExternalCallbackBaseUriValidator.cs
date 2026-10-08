namespace Elsa.ExternalAuthentication.Validation;

/// <summary>Validates the deployment-owned public callback base used by upstream identity providers.</summary>
public static class ExternalCallbackBaseUriValidator
{
    public static bool IsValid(Uri? uri, bool allowDevelopmentLoopback) =>
        uri is { IsAbsoluteUri: true } &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Fragment) &&
        string.IsNullOrEmpty(uri.Query) &&
        !uri.Host.Contains('*') &&
        (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
         allowDevelopmentLoopback && uri.IsLoopback && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));

    public static string ErrorMessage => "External Authentication Redirects:ExternalCallbackBaseUri must be an absolute HTTPS URI without credentials, a query, or a fragment. HTTP is allowed only for loopback callbacks when AllowDevelopmentLoopbackCallbacks is enabled.";
}
