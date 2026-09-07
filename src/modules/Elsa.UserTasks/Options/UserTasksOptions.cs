namespace Elsa.UserTasks.Options;

public sealed class UserTasksOptions
{
    public string DefaultTenantId { get; set; } = "";
    public string DefaultProvider { get; set; } = "default";
    public string SubjectClaimType { get; set; } = "sub";
    public string TenantClaimType { get; set; } = "tenant";
    public string ProviderClaimType { get; set; } = "elsa:identity-provider";
    public string DisplayNameClaimType { get; set; } = "name";
    public ICollection<string> GroupClaimTypes { get; set; } = ["groups", "group"];
    public ICollection<string> PermissionClaimTypes { get; set; } = ["permission", "permissions"];
    public int MaximumPayloadBytes { get; set; } = 256 * 1024;
    public TimeSpan DefaultInvitationLifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Upper bound on a guest session, applied on top of the invitation's own expiry.</summary>
    public TimeSpan GuestSessionLifetime { get; set; } = TimeSpan.FromHours(4);

    /// <summary>Attempts allowed per caller partition within <see cref="AnonymousRateLimitWindow"/>.</summary>
    public int AnonymousRateLimit { get; set; } = 10;

    public TimeSpan AnonymousRateLimitWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Back-off schedule for invitation delivery retries. Delivery is abandoned once the list is exhausted.</summary>
    public IReadOnlyList<TimeSpan> InvitationDeliveryRetryDelays { get; set; } =
        [TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)];

    /// <summary>Advertised to clients through the feature capability descriptor.</summary>
    public bool RealtimeEnabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 30;

    /// <summary>How often the hosted worker marks overdue tasks and applies the timeout outcome.</summary>
    public TimeSpan DueSweepInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>How often the hosted worker reconciles committed bookmarks against projected tasks.</summary>
    public TimeSpan ReconciliationInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Tenants swept by the hosted workers. Empty means the default tenant only.</summary>
    public ICollection<string> WorkerTenantIds { get; set; } = [];
}
