namespace Shared.Options
{
    public sealed class AuthenticationOptions
    {
        /// <summary>
        /// The URL of the /token endpoint.
        /// </summary>
        public string TokenEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the /authorize token endpoint.
        /// </summary>
        public string? AuthorizationEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// The client ID to use for the client.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Optionally, the client secret to use for the confidental client.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// The scopes of access being requested during the authentication process.
        /// Typically, this defines the permissions or resources the client is requesting
        /// authorization for.
        /// </summary>
        public string[] Scopes { get; set; } = [];

        /// <summary>
        /// Whether to use Proof Key for Code Exchange (PKCE) during the authentication process.
        /// </summary>
        public bool UsePkce { get; set; }
    }
}
