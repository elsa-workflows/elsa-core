namespace Elsa.Diagnostics.Options;

public class ServerLogStreamingOptions
{
    public int RecentLogCapacity { get; set; } = 5_000;
    public int SubscriberChannelCapacity { get; set; } = 1_000;
    public int MaxRecentLogQuerySize { get; set; } = 1_000;
    public TimeSpan SourceHeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IncludeDiagnosticsInternalLogs { get; set; }
    public ICollection<string> SensitiveNames { get; set; } =
    [
        "authorization",
        "token",
        "password",
        "secret",
        "api-key",
        "apikey",
        "cookie",
        "connection-string",
        "connectionstring"
    ];
}
