namespace Elsa.Diagnostics.StructuredLogs.Options;

public class StructuredLogsOptions
{
    public int RecentLogCapacity { get; set; } = 5_000;
    public int SubscriberChannelCapacity { get; set; } = 1_000;
    public int MaxRecentLogQuerySize { get; set; } = 1_000;
    public TimeSpan SourceHeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IncludeStructuredLogsInternalLogs { get; set; }
    
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
    
    public ICollection<string> SensitiveTextPatterns { get; set; } =
    [
        "(?i)bearer\\s+[A-Za-z0-9._~+/=-]+",
        "(?i)(password|secret|token|api[-_]?key)\\s*[=:]\\s*[^\\s,;]+",
        "(?i)(AccountKey|SharedAccessKey)=([^;\\s]+)"
    ];
}
