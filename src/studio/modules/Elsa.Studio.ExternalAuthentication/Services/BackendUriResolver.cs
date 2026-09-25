namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>
/// Resolves broker routes against the configured backend base URI without dropping its path prefix.
/// </summary>
public static class BackendUriResolver
{
    /// <summary>
    /// Resolves an absolute URI or a backend-relative route.
    /// Root-relative broker routes are intentionally resolved beneath the configured backend path.
    /// </summary>
    public static Uri Resolve(Uri backendUri, string location)
    {
        ArgumentNullException.ThrowIfNull(backendUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        if (Uri.TryCreate(location, UriKind.Absolute, out var absoluteUri) &&
            (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            return absoluteUri;

        var backendBuilder = new UriBuilder(backendUri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };
        if (!backendBuilder.Path.EndsWith('/'))
            backendBuilder.Path += '/';

        return new Uri(backendBuilder.Uri, location.TrimStart('/'));
    }

    /// <summary>
    /// Resolves a broker location and verifies that it remains on the configured backend origin.
    /// </summary>
    public static bool TryResolveSameOrigin(Uri backendUri, string? location, out Uri resolvedUri)
    {
        resolvedUri = default!;
        if (string.IsNullOrWhiteSpace(location))
            return false;

        try
        {
            var candidate = Resolve(backendUri, location);
            if (!IsSameOrigin(candidate, backendUri))
                return false;

            resolvedUri = candidate;
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    /// <summary>Returns whether two URIs have the same scheme, host, and port.</summary>
    public static bool IsSameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase) &&
        left.Port == right.Port;
}
