namespace Elsa.ExternalAuthentication.Constants;

/// <summary>
/// Stable Data Protection purpose segments for broker-owned opaque payloads.
/// </summary>
public static class ExternalAuthenticationDataProtectionPurposes
{
    public const string Root = "Elsa.ExternalAuthentication";
    public const string BrokerState = Root + ".BrokerState";
    public const string AdapterState = Root + ".AdapterState";
    public const string PreviewState = Root + ".PreviewState";
    public const string LogoutState = Root + ".LogoutState";
}
