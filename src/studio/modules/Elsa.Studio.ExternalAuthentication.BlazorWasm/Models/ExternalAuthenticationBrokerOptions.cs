using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;

/// <summary>
/// Configures Elsa Studio's public External Authentication broker client.
/// </summary>
public sealed class ExternalAuthenticationWasmOptions
{
    /// <summary>The deployment-managed public Authentication Client identifier.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The client-local path registered for authorization-code callbacks.</summary>
    public string CallbackPath { get; set; } = ExternalAuthenticationCallbackPaths.SignIn;

    /// <summary>The client-local path registered for logout callbacks.</summary>
    public string LogoutCallbackPath { get; set; } = ExternalAuthenticationCallbackPaths.Logout;

    /// <summary>
    /// Reserved solely to detect an invalid confidential-client configuration. Public WebAssembly clients never use it.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>The browser persistence policy for post-exchange credentials.</summary>
    public ExternalAuthenticationBrowserStorageMode BrowserStorage { get; set; }

    internal ExternalAuthenticationClientOptions ToClientOptions() => new()
    {
        ClientId = ClientId,
        CallbackPath = CallbackPath,
        LogoutCallbackPath = LogoutCallbackPath,
        SecurityWarning = BrowserStorage switch
        {
            ExternalAuthenticationBrowserStorageMode.Session => "This deployment stores authentication credentials in browser session storage. Close the browser tab when you finish.",
            ExternalAuthenticationBrowserStorageMode.Durable => "This deployment stores authentication credentials persistently in this browser. Sign out when you finish and avoid using shared devices.",
            _ => null
        }
    };
}

/// <summary>Controls where post-exchange credentials are retained by a WebAssembly host.</summary>
public enum ExternalAuthenticationBrowserStorageMode
{
    /// <summary>Keep credentials only in the running application process.</summary>
    Memory,

    /// <summary>Keep credentials in the current browser tab.</summary>
    Session,

    /// <summary>Keep credentials beyond the current tab and browser session.</summary>
    Durable
}
