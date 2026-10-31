using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Builds Elsa-owned upstream callback URIs without allowing a configured base path to be discarded.</summary>
public static class ExternalAuthenticationCallbackUris
{
    /// <summary>Returns the provider callback for a normal external sign-in or an administrator preview.</summary>
    public static Uri GetAuthorizationCallbackUri(Uri externalCallbackBaseUri, IdentityProviderConnection connection, BrokerTransactionPurpose purpose) => purpose switch
    {
        BrokerTransactionPurpose.ExternalSignIn => AppendPath(externalCallbackBaseUri, $"external-authentication/callback/{Uri.EscapeDataString(ConnectionRevisionCalculator.NormalizeKey(connection.Key))}"),
        BrokerTransactionPurpose.Preview => AppendPath(externalCallbackBaseUri, $"external-authentication/previews/callback/{Uri.EscapeDataString(connection.Id)}"),
        _ => throw new InvalidOperationException($"Broker transaction purpose '{purpose}' cannot initiate an upstream authorization callback.")
    };

    /// <summary>Returns the provider callback for a broker-initiated upstream logout.</summary>
    public static Uri GetLogoutCallbackUri(Uri externalCallbackBaseUri, string connectionKey) =>
        AppendPath(externalCallbackBaseUri, $"external-authentication/logout/callback/{Uri.EscapeDataString(ConnectionRevisionCalculator.NormalizeKey(connectionKey))}");

    private static Uri AppendPath(Uri externalCallbackBaseUri, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(externalCallbackBaseUri);

        var builder = new UriBuilder(externalCallbackBaseUri)
        {
            Path = $"{externalCallbackBaseUri.AbsolutePath.TrimEnd('/')}/{relativePath.TrimStart('/')}",
            Query = string.Empty,
            Fragment = string.Empty,
            Port = externalCallbackBaseUri.IsDefaultPort ? -1 : externalCallbackBaseUri.Port
        };
        return builder.Uri;
    }
}
