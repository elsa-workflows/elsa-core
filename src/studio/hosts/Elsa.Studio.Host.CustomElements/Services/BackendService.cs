namespace Elsa.Studio.Host.CustomElements.Services;

/// <summary>
/// Represents a service for backend configuration and information management.
/// </summary>
/// <remarks>
/// The <see cref="BackendService"/> class provides properties to manage backend connectivity,
/// including endpoint configuration, API key management, access token, and tenant identification.
/// It acts as a centralized configuration point for backend communication settings.
/// </remarks>
public class BackendService
{
    /// <summary>
    /// Gets or sets the remote endpoint URL for backend communication.
    /// </summary>
    /// <remarks>
    /// The <c>RemoteEndpoint</c> property specifies the base URL of the backend service that the application interacts with.
    /// It is used as a key configuration for defining the remote backend's address and is essential for facilitating communication with the service.
    /// This property is typically configured during application initialization or dynamically at runtime.
    /// </remarks>
    public string RemoteEndpoint { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API key used for authenticating requests to the backend service.
    /// </summary>
    /// <remarks>
    /// The <c>ApiKey</c> property is used to supply authentication credentials in the form of an API key
    /// when communicating with the backend service. It is typically included in the request headers to identify
    /// and authorize the client. This property should hold a secure, non-empty value if API key-based authentication
    /// is enabled for the backend. Configuring this property correctly is critical for ensuring authorized access
    /// to backend resources.
    /// </remarks>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the access token used for authentication in backend communication.
    /// </summary>
    /// <remarks>
    /// The <c>AccessToken</c> property specifies a bearer token used for authorizing requests to protected backend resources.
    /// This property is typically set during initialization or dynamically updated to ensure proper authorization headers are sent with HTTP requests.
    /// It is utilized when API key-based authentication is not provided, ensuring secure access through bearer token mechanisms.
    /// </remarks>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for backend communication.
    /// </summary>
    /// <remarks>
    /// The <c>TenantId</c> property specifies the unique identifier associated with the tenant context in the backend service.
    /// It is used to distinguish resources and operations specific to a particular tenant, enabling multi-tenancy support.
    /// This property can be set during initialization or dynamically altered at runtime to switch between tenants.
    /// Setting this property is not necessary if the <see cref="AccessToken"/> carries a claim that can be used on the back-end to determine the current tenant.
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the HTTP header used for tenant identification during backend communication.
    /// </summary>
    /// <remarks>
    /// The <c>TenantIdHeaderName</c> property defines the header key that carries the tenant identifier in HTTP requests.
    /// It is essential for multi-tenant applications to distinguish between different tenants while interacting with the backend service.
    /// This property can be customized to align with specific header naming conventions required by the backend.
    /// </remarks>
    public string TenantIdHeaderName { get; set; } = "X-Tenant-Id";
}